using PurePatcher.Process;

namespace Tests;

internal class TestPatches : Test {
    [OneTimeSetUp]
    public override void Setup() {
        base.Setup();

        FieldAdder.ProcessTypes([
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(NewFields)}"),
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(DefaultValues)}"),
            TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(Injections)}")
        ]);

        MethodReplacer.RunReplacements(Set);
        FreePatcher.RunPatches(Set, "TestAssemblyTarget");
        Reloader.Reload(Set, LoadAssembly);
    }

    [Test]
    public void TestNewFields() {
        Assert.Multiple(() => {
            Assert.That(NewFields.TestIntField(1), Is.EqualTo(1));
            Assert.That(NewFields.TestIntStructField(1), Is.EqualTo(1));
            Assert.That(NewFields.TestGenericField1("test1"), Is.EqualTo("test1"));
            Assert.That(NewFields.TestGenericField2("test2"), Is.EqualTo("test2"));
            Assert.That(NewFields.TestGenericField3("test3"), Is.EqualTo("test3"));
        });
    }

    [Test]
    public void TestInjections() {
        Assert.Multiple(() => {
            Assert.That(Injections.TestOtherCompInjection(), Is.True);
            Assert.That(Injections.TestCompInjection(), Is.True);
            Assert.That(Injections.TestCompInjection_DoubleInit(), Is.True);
            Assert.That(Injections.TestCompBaseInjection(), Is.True);
            Assert.That(Injections.TestCompInjectionOnSubType(), Is.True);
            Assert.That(Injections.TestCompBaseInjectionOnSubType(), Is.True);
            Assert.That(Injections.TestCompInjectionOnSuperType(), Is.True);
        });
    }

    [Test]
    public void TestDefaultValues() {
        var values = DefaultValues.TestDefaultValues();

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
    public void TestDefaultValueInitializers() {
        var values = DefaultValues.TestDefaultValueInitializers();

        Assert.Multiple(() => {
            Assert.That(values[0], Is.EqualTo(DefaultValues.IntParameterlessInitializer()));

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
    public void TestFreePatching() {
        Assert.Multiple(() => {
            Assert.That(FreePatching.TestRewriteTargetMethod(), Is.EqualTo(1));
            Assert.That(FreePatching.TestRewriteTargetMethod2(), Is.EqualTo("b"));
        });
    }

    [Test]
    public void TestMethodReplacement() {
        Assert.Multiple(() => {
            Assert.That(MethodReplacement.TestInstanceMethod(), Is.EqualTo(12));
            Assert.That(MethodReplacement.TestStaticMethod(), Is.EqualTo("ab"));
        });
    }
}