using System.Collections;
using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class BadFields {
    // The parameter type is ArrayList because it isn't resolvable in the test environment
    // (System assembly isn't provided)
    [AddField]
    private static extern ref int FailUnresolvable(ArrayList target);

    [AddField]
    private static extern ref int FailInterface(ITarget target);

    [AddField]
    [BindComponent]
    private static extern ref BaseComp FailInjectionByRef(BaseWithComps target);

    [AddField]
    [BindComponent]
    private static extern BaseComp FailUnknownInjection(TargetClass target);
}