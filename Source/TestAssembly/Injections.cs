using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class Injections {
    extension(BaseWithComps target) {
        [AddField]
        [BindComponent]
        private extern OtherComp SomeComp();

        [AddField]
        [BindComponent]
        private extern DerivedMyComponent MyComp();

        [AddField]
        [BindComponent]
        private extern MyComponent MyCompBase();
    }

    extension(DerivedWithComps target) {
        [AddField]
        [BindComponent]
        private extern DerivedMyComponent MyCompOnSubType();

        [AddField]
        [BindComponent]
        private extern MyComponent MyCompBaseOnSubType();
    }

    [AddField]
    [BindComponent]
    private static extern MyComponent MyCompBaseOnSuperType(this InjectionBase target);

    // Exact comp type, initializer type == target type
    public static bool TestOtherCompInjection() {
        var thing = new BaseWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        return thing.SomeComp() == thing.comps[1];
    }

    // Exact comp type, initializer type == target type
    public static bool TestCompInjection() {
        var thing = new BaseWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        return thing.MyComp() == thing.comps[0];
    }

    // Exact comp type, initializer type == target type
    public static bool TestCompInjection_DoubleInit() {
        var thing = new BaseWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        thing.InitComps();
        return thing.MyComp() == thing.comps[0];
    }

    // Sub comp type, initializer type == target type
    public static bool TestCompBaseInjection() {
        var thing = new BaseWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        return thing.MyCompBase() == thing.comps[0];
    }

    // Exact comp type, initializer type == super of target type
    public static bool TestCompInjectionOnSubType() {
        var thing = new DerivedWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        return thing.MyCompOnSubType() == thing.comps[0];
    }

    // Sub comp type, initializer type == super of target type
    public static bool TestCompBaseInjectionOnSubType() {
        var thing = new DerivedWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        return thing.MyCompBaseOnSubType() == thing.comps[0];
    }

    // Exact comp type, initializer type == sub of target type
    public static bool TestCompInjectionOnSuperType() {
        var thing = new DerivedWithComps { CompTypes = [typeof(DerivedMyComponent), typeof(OtherComp)] };
        thing.InitComps();
        return thing.MyCompBaseOnSuperType() == thing.comps[0];
    }
}

public class MyComponent : BaseComp;

public class DerivedMyComponent : MyComponent;