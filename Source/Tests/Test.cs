global using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using PurePatcher;
using PurePatcher.Process;
using Tests.Helpers;

namespace Tests;

internal class Test {
    protected AssemblySet Set = null!;
    protected FieldAdder FieldAdder = null!;
    protected ModifiableAssembly TestAsm = null!;
    protected ModifiableAssembly TargetAsm = null!;

    private readonly Dictionary<string, byte[]> _patchedBytes = new(StringComparer.OrdinalIgnoreCase);
    private AssemblyLoadContext? _patchedContext;
    private Assembly? _patchedTestAssembly;

    public virtual void Setup() {
        Logger.InfoFunc = Console.WriteLine;
        Logger.ErrorFunc = msg => {
            Console.WriteLine(msg);
            throw new LogErrorException($"{msg}");
        };

        var liveAsms = LoadLiveAsms();

        Set = new AssemblySet();
        FieldAdder = new FieldAdder(Set);

        TargetAsm = Set.AddAssembly("TestAssemblyTarget.dll", AssemblyPath("TestAssemblyTarget.dll"), null);
        TargetAsm.ProcessAttributes = true;
        TargetAsm.SourceAssembly = liveAsms.Target;

        TestAsm = Set.AddAssembly("TestAssembly.dll", AssemblyPath("TestAssembly.dll"), null);
        TestAsm.ProcessAttributes = true;
        TestAsm.SourceAssembly = liveAsms.Test;

        var typeThingWithComps = TargetAsm.ModuleDefinition.GetType("TestAssemblyTarget.BaseWithComps");
        var typeNullableWithComps = TargetAsm.ModuleDefinition.GetType("TestAssemblyTarget.NullableWithComps");
        var typeThingComp = TargetAsm.ModuleDefinition.GetType("TestAssemblyTarget.BaseComp");

        FieldAdder.RegisterInjection(
            typeThingWithComps,
            typeThingComp,
            "InitComps",
            "comps"
        );

        FieldAdder.RegisterInjection(
            typeNullableWithComps,
            typeThingComp,
            "InitComps",
            "comps"
        );
    }

    // Load the test assemblies and make them resolvable for assembly rewrite testing.
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
            foreach (var m in FieldAdder.GetAllAddFieldAccessors(testAsmToBeLive.Types)) {
                Util.SetEmptyBody(m);
            }

            var stream = new MemoryStream();
            testAsmToBeLive.Write(stream);
            liveTestAsm = Assembly.Load(stream.ToArray());
        }

        {
            testTargetAsmToBeLive.Name = testAssemblyTargetNewName;
            testTargetAsmToBeLive.Assembly.Name.Name = testAssemblyTargetNewName;

            foreach (var m in FieldAdder.GetAllAddFieldAccessors(testTargetAsmToBeLive.Types)) {
                Util.SetEmptyBody(m);
            }

            var stream = new MemoryStream();
            testTargetAsmToBeLive.Write(stream);
            liveTargetAsm = Assembly.Load(stream.ToArray());
        }

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) => args.Name.StartsWith(testAssemblyTargetNewName)
            ? liveTargetAsm
            : null;

        return (liveTestAsm, liveTargetAsm);
    }

    // The rewrite lands in files the game loads on its next start, so the patched assemblies have to be
    // loaded into a context of their own: the already loaded originals keep their empty accessor bodies.
    protected void LoadAssembly(ModifiableAssembly asm) {
        _patchedBytes[asm.AsmDefinition.Name.Name] = asm.Bytes!;
    }

    protected Assembly PatchedTestAssembly {
        get {
            if (_patchedTestAssembly != null) return _patchedTestAssembly;

            _patchedContext = new AssemblyLoadContext("patched", isCollectible: true);
            _patchedContext.Resolving += (context, name) =>
                _patchedBytes.TryGetValue(name.Name!, out var bytes)
                    ? context.LoadFromStream(new MemoryStream(bytes))
                    : null;

            // Load the target first: otherwise resolution falls back to the already loaded original.
            foreach (var patched in _patchedBytes.Where(entry => entry.Key != "TestAssembly")) {
                _patchedContext.LoadFromStream(new MemoryStream(patched.Value));
            }

            return _patchedTestAssembly = _patchedContext.LoadFromStream(new MemoryStream(_patchedBytes["TestAssembly"]));
        }
    }

    protected T InvokePatched<T>(string typeName, string methodName, params object?[] arguments) =>
        (T)PatchedTestAssembly.GetType(typeName, throwOnError: true)!
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, arguments)!;

    private static string AssemblyPath(string fileName) => Path.Combine(AppContext.BaseDirectory, fileName);
}