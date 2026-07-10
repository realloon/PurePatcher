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
        foreach (var accessor in FieldAdder.GetAllAddFieldAccessors(TestExtensions.EnumerableOf(_typeFail))) {
            Assert.Throws<InvalidOperationException>(() => { FieldAdder.ProcessAccessor(accessor); }, accessor.Name);
        }
    }

    [Test]
    public void TestAssemblyRewriteFailureIsFatal() {
        var rewriter = typeof(TestExceptions).GetMethod(
            nameof(FailingAssemblyRewrite),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AssemblyRewriter.InvokeRewriter(rewriter, TargetAsm.ModuleDefinition));

        Assert.Multiple(() => {
            Assert.That(exception!.Message, Does.Contain(nameof(FailingAssemblyRewrite)));
            Assert.That(exception.InnerException, Is.TypeOf<ApplicationException>());
            Assert.That(exception.InnerException!.Message, Is.EqualTo("Expected rewrite failure"));
        });
    }

    private static void FailingAssemblyRewrite(ModuleDefinition _) =>
        throw new ApplicationException("Expected rewrite failure");
}