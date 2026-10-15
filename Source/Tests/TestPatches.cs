using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PurePatcher.Process;
using PurePatcher.Runtime;
using TestAssemblyTarget;

namespace Tests;

internal class TestPatches : Test {
    [OneTimeSetUp]
    public override void Setup() {
        base.Setup();

        FieldAdder.ProcessTypes([
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(NewFields)}"),
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(DefaultValues)}"),
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(Injections)}"),
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(SameModuleAccess)}")
        ]);

        MethodReplacer.RunReplacements(Set);
        AssemblyRewriter.RunRewrites(Set, "TestAssemblyTarget");

        foreach (var assembly in Set.AllAssemblies.Where(candidate => candidate.Modified)) {
            AssemblyInjection.InjectAccessCheckExemptions(assembly.ModuleDefinition);
        }

        foreach (var assembly in Set.AllAssemblies.Where(candidate => candidate.Modified)) {
            assembly.SerializeToByteArray();
            LoadAssembly(assembly);
        }
    }

    [Test]
    public void TestNewFields() {
        Assert.Multiple(() => {
            Assert.That(InvokePatched<int>(PatchType(nameof(NewFields)), nameof(NewFields.TestIntField), 1),
                Is.EqualTo(1));
            Assert.That(InvokePatched<int>(PatchType(nameof(NewFields)), nameof(NewFields.TestIntStructField), 1),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void TestInjections() {
        Assert.Multiple(() => {
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestOtherCompInjection)),
                Is.True);
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompInjection)),
                Is.True);
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompInjection_DoubleInit)),
                Is.True);
            Assert.That(
                InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompInjection_ClearedOnReinit)),
                Is.True);
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompBaseInjection)),
                Is.True);
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompInjectionOnSubType)),
                Is.True);
            Assert.That(
                InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompBaseInjectionOnSubType)),
                Is.True);
            Assert.That(
                InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestCompInjectionOnSuperType)),
                Is.True);
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestNullCompsInjection)),
                Is.True);
            Assert.That(InvokePatched<bool>(PatchType(nameof(Injections)), nameof(Injections.TestNullableCompsInjection)),
                Is.True);
        });
    }

    [Test]
    public void TestComponentBindingsAreBatched() {
        var initMethod = TargetAsm.ModuleDefinition
            .GetType($"{nameof(TestAssemblyTarget)}.{nameof(BaseWithComps)}")
            .Methods.Single(method => method.Name == nameof(BaseWithComps.InitComps));
        var listReads = initMethod.Body.Instructions
            .Select(instruction => instruction.Operand)
            .OfType<MethodReference>()
            .Count(reference => reference.DeclaringType.FullName == typeof(System.Collections.IList).FullName &&
                                reference.Name == "get_Item");

        Assert.That(listReads, Is.EqualTo(1));
    }

    [Test]
    public void TestDefaultValues() {
        var values = InvokePatched<object?[]>(PatchType(nameof(DefaultValues)), nameof(DefaultValues.TestDefaultValues));

        Assert.Multiple(() => {
            Assert.That(values[0], Is.EqualTo(false));
            Assert.That(values[1], Is.EqualTo(true));

            Assert.That(values[2], Is.EqualTo(int.MinValue));
            Assert.That(values[3], Is.EqualTo(int.MaxValue));
            Assert.That(values[4], Is.EqualTo(0));

            Assert.That(values[5], Is.EqualTo(uint.MinValue));
            Assert.That(values[6], Is.EqualTo(uint.MaxValue));
            Assert.That(values[7], Is.EqualTo(0u));

            Assert.That(values[8], Is.EqualTo(long.MinValue));
            Assert.That(values[9], Is.EqualTo(long.MaxValue));
            Assert.That(values[10], Is.EqualTo(0L));

            Assert.That(values[11], Is.EqualTo(ulong.MinValue));
            Assert.That(values[12], Is.EqualTo(ulong.MaxValue));
            Assert.That(values[13], Is.EqualTo(0UL));

            Assert.That(values[14], Is.EqualTo(float.MinValue));
            Assert.That(values[15], Is.EqualTo(float.MaxValue));
            Assert.That(values[16], Is.EqualTo(0f));

            Assert.That(values[17], Is.EqualTo(double.MinValue));
            Assert.That(values[18], Is.EqualTo(double.MaxValue));
            Assert.That(values[19], Is.EqualTo(0d));

            Assert.That(values[20], Is.EqualTo("a"));
            Assert.That(values[21], Is.EqualTo(null));
        });
    }

    [Test]
    public void TestDefaultInitValues() {
        var values = InvokePatched<object?[]>(PatchType(nameof(DefaultValues)),
            nameof(DefaultValues.TestDefaultInitValues));

        Assert.Multiple(() => {
            Assert.That(values[0], Is.EqualTo(1));

            Assert.That(values[1], Is.EqualTo(1));
            Assert.That(values[2], Is.EqualTo(true));

            Assert.That(values[3], Is.EqualTo(true));
            Assert.That(values[4], Is.EqualTo(1));
            Assert.That(values[5], Is.EqualTo(1));
            Assert.That(values[6], Is.EqualTo(1));
            Assert.That(values[7], Is.EqualTo(1));
            Assert.That(values[8], Is.EqualTo(1));
            Assert.That(values[9], Is.EqualTo(1));
        });
    }

    [Test]
    public void TestAssemblyRewriting() {
        Assert.That(InvokePatched<int>(PatchType(nameof(AssemblyRewriting)),
            nameof(AssemblyRewriting.TestRewriteTargetMethod)), Is.EqualTo(1));
    }

    [Test]
    public void TestMethodReplacement() {
        var type = PatchType(nameof(MethodReplacement));
        Assert.Multiple(() => {
            Assert.That(InvokePatched<int>(type, nameof(MethodReplacement.TestInstanceMethod)), Is.EqualTo(12));
            Assert.That(InvokePatched<string>(type, nameof(MethodReplacement.TestStaticMethod)), Is.EqualTo("ab"));
            Assert.That(InvokePatched<int>(type, nameof(MethodReplacement.TestBranchMethod), 5), Is.EqualTo(12));
            Assert.That(InvokePatched<int>(type, nameof(MethodReplacement.TestBranchMethod), 1), Is.EqualTo(-8));
        });
    }

    [Test]
    public void TestSameModuleInitializerIsWidenedAndCallable() {
        var initializer = TestAsm.ModuleDefinition.GetType(PatchType(nameof(SameModuleAccess)))
            .Methods.Single(method => method.Name == "PrivateInitializer");

        Assert.Multiple(() => {
            Assert.That(initializer.IsAssembly, Is.True);
            Assert.That(InvokePatched<int>(PatchType(nameof(SameModuleAccess)), nameof(SameModuleAccess.Read)),
                Is.EqualTo(41));
        });
    }

    [Test]
    public void TestAccessCheckExemptionsCoverReferences() {
        foreach (var assembly in Set.AllAssemblies.Where(candidate => candidate.Modified)) {
            var module = assembly.ModuleDefinition;
            var exempt = module.Assembly.CustomAttributes
                .Where(attribute => attribute.AttributeType.FullName == AssemblyInjection.AttributeTypeName)
                .Select(attribute => (string)attribute.ConstructorArguments[0].Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Assert.Multiple(() => {
                Assert.That(module.Types.Count(type => type.Name == "IgnoresAccessChecksToAttribute"), Is.EqualTo(1));
                Assert.That(module.AssemblyReferences.Select(reference => reference.Name), Is.SubsetOf(exempt));
            });
        }
    }

    [Test]
    public void TestUnresolvableReferencesAreSkipped() {
        var method = TestAsm.ModuleDefinition.GetType(PatchType(nameof(SameModuleAccess)))
            .Methods.Single(candidate => candidate.Name == nameof(SameModuleAccess.Read));
        var bogus = new TypeReference("Bogus", "Missing", TestAsm.ModuleDefinition,
            new AssemblyNameReference("Bogus", new Version(1, 0)));
        method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Isinst, bogus));

        Assert.DoesNotThrow(() => AccessWidening.WidenSameModuleReferences(method));
    }

    private static string PatchType(string name) => $"{nameof(Tests)}.{name}";

    [Test]
    public void TestMethodReplacementIsInlined() {
        var method = ReplaceMethodTargetMethod(nameof(ReplaceMethodTarget.InstanceMethod));
        var replacementCalls = method.Body.Instructions
            .Select(instruction => instruction.Operand)
            .OfType<MethodReference>()
            .Where(reference => reference.DeclaringType.FullName == $"{nameof(Tests)}.{nameof(MethodReplacement)}")
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Multiple(() => {
            Assert.That(replacementCalls, Does.Not.Contain(nameof(MethodReplacement.ReplaceInstanceMethod)));
            Assert.That(method.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Add), Is.True);
        });
    }

    private MethodDefinition ReplaceMethodTargetMethod(string methodName) {
        var type = TargetAsm.ModuleDefinition.GetType(
            $"{nameof(TestAssemblyTarget)}.{nameof(ReplaceMethodTarget)}");
        return type.Methods.Single(method => method.Name == methodName);
    }
}