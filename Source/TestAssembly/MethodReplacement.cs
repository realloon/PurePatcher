using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class MethodReplacement {
    // ReSharper disable once MemberCanBePrivate.Global
    public const string InactivePackageId = "PurePatcher.Tests.Inactive";
    public const string ActivePackageId = "PurePatcher.Tests.Active";

    public static int TestInstanceMethod() => new ReplaceMethodTarget().InstanceMethod(5);

    public static string TestStaticMethod() => ReplaceMethodTarget.StaticMethod("a");

    public static int TestBranchMethod(int value) => new ReplaceMethodTarget().BranchMethod(value);

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.InstanceMethod))]
    public static int ReplaceInstanceMethod(ReplaceMethodTarget self, int value) {
        return ReplaceMethodTarget.BaseValue + value;
    }

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.StaticMethod))]
    public static string ReplaceStaticMethod(string value) {
        return value + "b";
    }

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.BranchMethod))]
    public static int ReplaceBranchMethod(ReplaceMethodTarget self, int value) {
        var result = value + ReplaceMethodTarget.BaseValue;
        if (result > 10) {
            return result;
        }

        return -result;
    }

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.ActiveModConditionMethod))]
    [DisabledIfModActive(ActivePackageId)]
    public static int ReplaceActiveModConditionMethod(ReplaceMethodTarget self) {
        return 2;
    }

    [ReplaceMethod(typeof(ReplaceMethodTarget), nameof(ReplaceMethodTarget.InactiveModConditionMethod))]
    [DisabledIfModActive(InactivePackageId)]
    public static int ReplaceInactiveModConditionMethod(ReplaceMethodTarget self) {
        return 2;
    }
}