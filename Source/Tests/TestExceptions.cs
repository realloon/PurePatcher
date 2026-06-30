using Mono.Cecil;
using PurePatcher.Process;
using Tests.Helpers;

namespace Tests;

internal class TestExceptions : Test {
    private TypeDefinition typeFail = null!;

    [OneTimeSetUp]
    public override void Setup() {
        base.Setup();
        typeFail = testAsm.ModuleDefinition.GetType($"{nameof(Tests)}.{nameof(BadFields)}");
    }

    [Test]
    public void TestBadFieldAccessors() {
        foreach (var accessor in FieldAdder.GetAllPurePatcherFieldAccessors(TestExtensions.EnumerableOf(typeFail))) {
            Assert.Throws<LogErrorException>(() => { fieldAdder.ProcessAccessor(accessor); }, accessor.Name);
        }
    }
}
