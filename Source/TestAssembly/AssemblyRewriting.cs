using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class AssemblyRewriting {
    public static int TestRewriteTargetMethod() => new RewriteTarget().Method();

    [RewriteAssembly]
    public static void RewriteAssembly(ModuleDefinition module) {
        var type = module.GetType($"{nameof(TestAssemblyTarget)}.{nameof(RewriteTarget)}")!;
        var method = type.FindMethod(nameof(RewriteTarget.Method))!;

        foreach (var inst in method.Body.Instructions) {
            if (inst.OpCode == OpCodes.Ldc_I4_0) {
                inst.OpCode = OpCodes.Ldc_I4_1;
            }
        }
    }
}