using System.Collections.Generic;
using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class BadFields {
    [PurePatcherField]
    private static extern ref T FailGenericMethod<T>(TargetClass target);

    [PurePatcherField]
    private static extern ref T FailNestedGeneric<T>(TargetGeneric<List<T>> target);

    [PurePatcherField]
    private static extern ref int FailConcreteGeneric(TargetGeneric<int> target);

    [PurePatcherField]
    private static extern ref int FailGenericCount<T, U>(TargetGeneric<T> target);

    [PurePatcherField]
    private static extern ref int FailGenericsDontMatch<T>(TargetGeneric3<T, T, T> target);

    [PurePatcherField]
    private static extern ref int FailGenericsDontMatch<T, U, W>(TargetGeneric3<T, W, U> target);

    // The parameter type is List because it isn't resolvable in the test environment
    // (System assembly isn't provided)
    [PurePatcherField]
    private static extern ref int FailUnresolvable<T>(List<T> target);

    [PurePatcherField]
    private static extern ref int FailInterface(ITarget target);

    [PurePatcherField]
    [InjectComponent]
    private static extern ref BaseComp FailInjectionByRef(BaseWithComps target);

    [PurePatcherField]
    [InjectComponent]
    private static extern BaseComp FailUnknownInjection(TargetClass target);
}