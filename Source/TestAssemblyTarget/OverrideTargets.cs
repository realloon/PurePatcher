namespace TestAssemblyTarget;

public class OverrideBase {
    public static int IntNonVirtual() {
        return 1;
    }

    public virtual int IntVirtual() {
        return 1;
    }

    public static int IntNonVirtualArg(int a) {
        return a;
    }

    public int IntNonVirtual_Instance() {
        return 1;
    }

    protected static int IntNonVirtualArg_Instance(int a) {
        return a;
    }
}

public class OverrideMid : OverrideBase;