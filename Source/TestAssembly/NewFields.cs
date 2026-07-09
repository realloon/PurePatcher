using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class NewFields {
    [AddField]
    private static extern ref int MyInt(this TargetClass target);

    [AddField]
    private static extern ref int MyIntStruct(this ref TargetStruct target);

    public static int TestIntField(int i) {
        var obj = new TargetClass();
        obj.MyInt() = i;
        return obj.MyInt();
    }

    public static int TestIntStructField(int i) {
        TargetStruct s = default;
        s.MyIntStruct() = i;
        return s.MyIntStruct();
    }
}