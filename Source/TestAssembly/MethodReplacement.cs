using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class MethodReplacement {
    public static int TestInstanceMethod() => new ReplaceMethodTarget().InstanceMethod(5);

    public static string TestStaticMethod() => ReplaceMethodTarget.StaticMethod("a");

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.InstanceMethod))]
    public static int ReplaceInstanceMethod(ReplaceMethodTarget self, int value) {
        return ReplaceMethodTarget.BaseValue + value;
    }

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.StaticMethod))]
    public static string ReplaceStaticMethod(string value) {
        return value + "b";
    }
}