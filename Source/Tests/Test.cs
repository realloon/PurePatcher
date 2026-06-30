global using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using PurePatcher;
using PurePatcher.Process;
using TestAssemblyTarget;
using Tests.Helpers;

namespace Tests;

internal class Test {
    protected AssemblySet set = null!;
    protected FieldAdder fieldAdder = null!;
    protected ModifiableAssembly testAsm = null!;

    public virtual void Setup() {
        Lg.InfoFunc = Console.WriteLine;
        Lg.ErrorFunc = msg => {
            Console.WriteLine(msg);
            throw new LogErrorException($"{msg}");
        };

        var liveTestAsm = LoadLiveAsms();

        set = new AssemblySet();
        fieldAdder = new FieldAdder(set);

        var targetAsm = set.AddAssembly("Test", "TestAssemblyTarget.dll", "TestAssemblyTarget.dll", null);
        targetAsm.ProcessAttributes = true;

        testAsm = set.AddAssembly("Test", "TestAssembly.dll", "TestAssembly.dll", null);
        testAsm.ProcessAttributes = true;
        testAsm.SourceAssembly = liveTestAsm;

        var typeThingWithComps =
            targetAsm.ModuleDefinition.GetType($"{nameof(TestAssemblyTarget)}.{nameof(BaseWithComps)}");
        var typeThingComp =
            targetAsm.ModuleDefinition.GetType($"{nameof(TestAssemblyTarget)}.{nameof(BaseComp)}");

        fieldAdder.RegisterInjection(
            typeThingWithComps,
            typeThingComp,
            nameof(BaseWithComps.InitComps),
            nameof(BaseWithComps.comps)
        );
    }

    // Load the test assemblies and make them resolvable for freepatch testing
    private static Assembly LoadLiveAsms() {
        const string testAssemblyTargetNewName = "TestAssemblyTarget1";

        using var testAsmToBeLive = ModuleDefinition.ReadModule("TestAssembly.dll");
        using var testTargetAsmToBeLive = ModuleDefinition.ReadModule("TestAssemblyTarget.dll");
        Assembly liveTestAsm;
        Assembly liveTargetAsm;

        // Rename the referenced assembly so it isn't loaded from disk and passes through AssemblyResolve
        {
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

        return liveTestAsm;
    }

    protected static void WriteAssembly(ModifiableAssembly asm) {
        // Replaces the actual assembly file that will get autoloaded by the runtime
        File.WriteAllBytes(asm.AsmDefinition.ShortName() + ".dll", asm.Bytes!);
    }
}