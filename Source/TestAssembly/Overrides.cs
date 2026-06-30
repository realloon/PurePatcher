using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class Overrides {
    [PurePatcherOverride]
    public static int IntNonVirtual(OverrideMid obj) => OverrideBase.IntNonVirtual();

    [PurePatcherOverride]
    public static int IntNonVirtualArg(OverrideMid obj, int a) => OverrideBase.IntNonVirtualArg(a);

    [PurePatcherOverride]
    public static int IntVirtual(OverrideMid obj) => obj.IntVirtual();
}

public class OverrideSub : OverrideMid {
    [PurePatcherOverride]
    public new int IntNonVirtualArg_Instance(int a) {
        return OverrideBase.IntNonVirtualArg_Instance(a) + 1;
    }
}