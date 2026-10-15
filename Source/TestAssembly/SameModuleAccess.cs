using PurePatcher.Annotations;

namespace Tests;

public class SameModuleTarget;

public static class SameModuleAccess {
    [AddField]
    [InitValue(nameof(PrivateInitializer))]
    public static extern ref int SameModuleValue(this SameModuleTarget target);

    private static int PrivateInitializer() => 41;

    public static int Read() => new SameModuleTarget().SameModuleValue();
}
