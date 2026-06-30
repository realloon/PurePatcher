// ReSharper disable UnusedTypeParameter

namespace TestAssemblyTarget;

public class TargetClass;

public class SecondTargetClass(TargetClass inner) {
    public readonly TargetClass Inner = inner;
}

public struct TargetStruct;

public class TargetGeneric<T>;

public class TargetGeneric3<T, TU, TW>;

public interface ITarget;