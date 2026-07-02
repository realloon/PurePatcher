using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using PurePatcher.Process;
using TestAssemblyTarget;

namespace Tests;

internal class TestConditionalReplacements : Test {
    [OneTimeSetUp]
    public override void Setup() {
        base.Setup();

        MethodReplacer.RunReplacements(Set, new HashSet<string> { MethodReplacement.ActivePackageId });
    }

    [Test]
    public void TestReplaceMethodDisabledByActiveMod() {
        Assert.That(ReturnedIntConstant(nameof(ReplaceMethodTarget.ActiveModConditionMethod)), Is.EqualTo(1));
    }

    [Test]
    public void TestReplaceMethodRunsWhenDisabledModInactive() {
        Assert.That(ReturnedIntConstant(nameof(ReplaceMethodTarget.InactiveModConditionMethod)), Is.EqualTo(2));
    }

    private int ReturnedIntConstant(string methodName) {
        var instruction = ReplaceMethodTargetMethod(methodName).Body.Instructions
            .Single(instruction => instruction.OpCode == OpCodes.Ldc_I4_1 ||
                                   instruction.OpCode == OpCodes.Ldc_I4_2);

        return instruction.OpCode == OpCodes.Ldc_I4_1 ? 1 : 2;
    }

    private MethodDefinition ReplaceMethodTargetMethod(string methodName) {
        var type = TargetAsm.ModuleDefinition.GetType(
            $"{nameof(TestAssemblyTarget)}.{nameof(ReplaceMethodTarget)}");
        return type.Methods.Single(method => method.Name == methodName);
    }
}