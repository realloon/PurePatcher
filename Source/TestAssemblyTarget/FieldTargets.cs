namespace TestAssemblyTarget;

public class TargetClass;

public class SecondTargetClass(TargetClass inner) {
    public readonly TargetClass Inner = inner;
}

public struct TargetStruct;

public interface ITarget;