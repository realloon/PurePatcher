using System.Collections.Generic;
using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class NewFields {
    [PurePatcherField]
    private static extern ref int MyInt(this TargetClass target);

    [PurePatcherField]
    private static extern ref int MyIntStruct(this ref TargetStruct target);

    [PurePatcherField]
    private static extern ref List<T> MyList<T>(this TargetGeneric<T> target);

    extension<T, TU, TW>(TargetGeneric3<T, TU, TW> b) {
        [PurePatcherField]
        private extern ref (T, TW, TU) MyTriple();

        [PurePatcherField]
        private extern ref (T, T) MyPair();
    }

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

    public static string TestGenericField1(string s) {
        var obj = new TargetGeneric<string>();
        obj.MyList() = [];
        obj.MyList().Add(s);
        return obj.MyList()[0];
    }

    public static string TestGenericField2(string s) {
        var obj = new TargetGeneric3<string, int, float>();
        obj.MyTriple() = (s, 1f, 1);
        return obj.MyTriple().Item1;
    }

    public static string TestGenericField3(string s) {
        var obj = new TargetGeneric3<string, int, float>();
        obj.MyPair() = (s, s);
        return obj.MyPair().Item1;
    }
}