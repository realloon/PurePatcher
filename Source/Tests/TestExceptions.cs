using Mono.Cecil;
using PurePatcher.Process;
using Tests.Helpers;

namespace Tests;

internal class TestExceptions : Test {
    private TypeDefinition _typeFail = null!;

    [OneTimeSetUp]
    public override void Setup() {
        base.Setup();
        _typeFail = TestAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(BadFields)}");
    }

    [Test]
    public void TestBadFieldAccessors() {
        foreach (var accessor in FieldAdder.GetAllPurePatcherFieldAccessors(TestExtensions.EnumerableOf(_typeFail))) {
            Assert.Throws<LogErrorException>(() => { FieldAdder.ProcessAccessor(accessor); }, accessor.Name);
        }
    }
}
