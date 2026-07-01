namespace TestAssemblyTarget;

public class RewriteTarget {
    public int Method() => 0;

    public string Method2() => "a";
}

public class ReplaceMethodTarget {
    public const int BaseValue = 7;

    public int InstanceMethod(int value) => value;

    public static string StaticMethod(string value) => value;

    public int BranchMethod(int value) => value;
}