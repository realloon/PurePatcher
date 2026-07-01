using System.Collections.Generic;
using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class BadFields {
    [AddField]
    private static extern ref T FailGenericMethod<T>(TargetClass target);

    [AddField]
    private static extern ref T FailNestedGeneric<T>(TargetGeneric<List<T>> target);

    [AddField]
    private static extern ref int FailConcreteGeneric(TargetGeneric<int> target);

    [AddField]
    private static extern ref int FailGenericCount<T, TU>(TargetGeneric<T> target);

    [AddField]
    private static extern ref int FailGenericsDontMatch<T>(TargetGeneric3<T, T, T> target);

    [AddField]
    private static extern ref int FailGenericsDontMatch<T, TU, TW>(TargetGeneric3<T, TW, TU> target);

    // The parameter type is List because it isn't resolvable in the test environment
    // (System assembly isn't provided)
    [AddField]
    private static extern ref int FailUnresolvable<T>(List<T> target);

    [AddField]
    private static extern ref int FailInterface(ITarget target);

    [AddField]
    [BindComponent]
    private static extern ref BaseComp FailInjectionByRef(BaseWithComps target);

    [AddField]
    [BindComponent]
    private static extern BaseComp FailUnknownInjection(TargetClass target);
}