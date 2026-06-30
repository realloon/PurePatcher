global using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using PurePatcher;
using PurePatcher.Process;
using Tests.Helpers;

namespace Tests;

internal class Test {
    protected AssemblySet Set = null!;
    protected FieldAdder FieldAdder = null!;
    protected ModifiableAssembly TestAsm = null!;

    public virtual void Setup() {
        Logger.InfoFunc = Console.WriteLine;
        Logger.ErrorFunc = msg => {
            Console.WriteLine(msg);
            throw new LogErrorException($"{msg}");
        };

        var liveAsms = LoadLiveAsms();

        Set = new AssemblySet();
        FieldAdder = new FieldAdder(Set);

        var targetAsm = Set.AddAssembly("TestAssemblyTarget.dll", AssemblyPath("TestAssemblyTarget.dll"), null);
        targetAsm.ProcessAttributes = true;
        targetAsm.SourceAssembly = liveAsms.Target;

        TestAsm = Set.AddAssembly("TestAssembly.dll", AssemblyPath("TestAssembly.dll"), null);
        TestAsm.ProcessAttributes = true;
        TestAsm.SourceAssembly = liveAsms.Test;

        var typeThingWithComps = targetAsm.ModuleDefinition.GetType("TestAssemblyTarget.BaseWithComps");
        var typeThingComp = targetAsm.ModuleDefinition.GetType("TestAssemblyTarget.BaseComp");

        FieldAdder.RegisterInjection(
            typeThingWithComps,
            typeThingComp,
            "InitComps",
            "comps"
        );
    }

    // Load the test assemblies and make them resolvable for freepatch testing
    private static (Assembly Test, Assembly Target) LoadLiveAsms() {
        const string testAssemblyNewName = "TestAssembly1";
        const string testAssemblyTargetNewName = "TestAssemblyTarget1";

        using var testAsmToBeLive = ModuleDefinition.ReadModule(AssemblyPath("TestAssembly.dll"));
        using var testTargetAsmToBeLive = ModuleDefinition.ReadModule(AssemblyPath("TestAssemblyTarget.dll"));
        Assembly liveTestAsm;
        Assembly liveTargetAsm;

        // Rename the referenced assembly so it isn't loaded from disk and passes through AssemblyResolve
        {
            testAsmToBeLive.Name = testAssemblyNewName;
            testAsmToBeLive.Assembly.Name.Name = testAssemblyNewName;
            testAsmToBeLive.AssemblyReferences.First(a => a.Name == "TestAssemblyTarget").Name =
                testAssemblyTargetNewName;

            // The bodies have to be initialized because the test runtime doesn't seem to like non pinvoke extern methods
            // Mono is fine with them
            foreach (var m in FieldAdder.GetAllPurePatcherFieldAccessors(testAsmToBeLive.Types)) {
                Util.SetEmptyBody(m);
            }

            var stream = new MemoryStream();
            testAsmToBeLive.Write(stream);
            liveTestAsm = Assembly.Load(stream.ToArray());
        }

        {
            testTargetAsmToBeLive.Name = testAssemblyTargetNewName;
            testTargetAsmToBeLive.Assembly.Name.Name = testAssemblyTargetNewName;

            foreach (var m in FieldAdder.GetAllPurePatcherFieldAccessors(testTargetAsmToBeLive.Types))
                Util.SetEmptyBody(m);

            var stream = new MemoryStream();
            testTargetAsmToBeLive.Write(stream);
            liveTargetAsm = Assembly.Load(stream.ToArray());
        }

        AppDomain.CurrentDomain.AssemblyResolve +=
            (_, args) => args.Name.StartsWith(testAssemblyTargetNewName) ? liveTargetAsm : null;

        return (liveTestAsm, liveTargetAsm);
    }

    protected static void LoadAssembly(ModifiableAssembly asm) {
        Assembly.Load(asm.Bytes!);
    }

    private static string AssemblyPath(string fileName) {
        return Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
