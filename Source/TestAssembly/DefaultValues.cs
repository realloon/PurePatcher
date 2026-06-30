using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class DefaultValues {
    // bool defaults
    extension(TargetClass target) {
        [PurePatcherField]
        [DefaultValue(false)]
        public extern ref bool MyBoolDefaultFalse();

        [PurePatcherField]
        [DefaultValue(true)]
        public extern ref bool MyBoolDefaultTrue();

        [PurePatcherField]
        [DefaultValue(int.MinValue)]
        public extern ref int MyIntDefaultMin();

        [PurePatcherField]
        [DefaultValue(int.MaxValue)]
        public extern ref int MyIntDefaultMax();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref int MyIntDefaultNull();

        [PurePatcherField]
        [DefaultValue(uint.MinValue)]
        public extern ref uint MyUIntDefaultMin();

        [PurePatcherField]
        [DefaultValue(uint.MaxValue)]
        public extern ref uint MyUIntDefaultMax();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref uint MyUIntDefaultNull();

        [PurePatcherField]
        [DefaultValue(long.MinValue)]
        public extern ref long MyLongDefaultMin();

        [PurePatcherField]
        [DefaultValue(long.MaxValue)]
        public extern ref long MyLongDefaultMax();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref long MyLongDefaultNull();

        [PurePatcherField]
        [DefaultValue(ulong.MinValue)]
        public extern ref ulong MyULongDefaultMin();

        [PurePatcherField]
        [DefaultValue(ulong.MaxValue)]
        public extern ref ulong MyULongDefaultMax();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref ulong MyULongDefaultNull();

        [PurePatcherField]
        [DefaultValue(float.MinValue)]
        public extern ref float MyFloatDefaultMin();

        [PurePatcherField]
        [DefaultValue(float.MaxValue)]
        public extern ref float MyFloatDefaultMax();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref float MyFloatDefaultNull();

        [PurePatcherField]
        [DefaultValue(double.MinValue)]
        public extern ref double MyDoubleDefaultMin();

        [PurePatcherField]
        [DefaultValue(double.MaxValue)]
        public extern ref double MyDoubleDefaultMax();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref double MyDoubleDefaultNull();

        [PurePatcherField]
        [DefaultValue(null)]
        public extern ref string MyStringDefaultNull();

        [PurePatcherField]
        [DefaultValue("a")]
        public extern ref string MyStringDefault();

        [PurePatcherField]
        [ValueInitializer(nameof(IntParameterlessInitializer))]
        public extern ref int MyIntParameterless();

        [PurePatcherField]
        [ValueInitializer(nameof(IntThisInitializer))]
        public extern ref int MyIntFromThis();

        [PurePatcherField]
        [ValueInitializer(nameof(ObjectThisInitializer))]
        public extern ref SecondTargetClass MyObjectFromThis();
    }

    [PurePatcherField]
    [ValueInitializer(nameof(CounterInitializer))]
    public static extern ref int MyIntCounter(this DerivedCtorsClass target);

    public static int IntParameterlessInitializer() => 1;

    public static int IntThisInitializer(TargetClass? obj) => obj != null ? 1 : -1;

    public static SecondTargetClass ObjectThisInitializer(TargetClass obj) => new(obj);

    public static int CounterInitializer(DerivedCtorsClass ctors) => ++ctors.counter;

    public static object?[] TestDefaultValues() {
        var target = new TargetClass();
        return [
            target.MyBoolDefaultFalse(),
            target.MyBoolDefaultTrue(),
            target.MyIntDefaultMin(),
            target.MyIntDefaultMax(),
            target.MyIntDefaultNull(),
            target.MyUIntDefaultMin(),
            target.MyUIntDefaultMax(),
            target.MyUIntDefaultNull(),
            target.MyLongDefaultMin(),
            target.MyLongDefaultMax(),
            target.MyLongDefaultNull(),
            target.MyULongDefaultMin(),
            target.MyULongDefaultMax(),
            target.MyULongDefaultNull(),
            target.MyFloatDefaultMin(),
            target.MyFloatDefaultMax(),
            target.MyFloatDefaultNull(),
            target.MyDoubleDefaultMin(),
            target.MyDoubleDefaultMax(),
            target.MyDoubleDefaultNull(),
            target.MyStringDefault(),
            target.MyStringDefaultNull()
        ];
    }

    public static object?[] TestDefaultValueInitializers() {
        var target = new TargetClass();
        return [
            target.MyIntParameterless(),
            target.MyIntFromThis(),
            ReferenceEquals(target.MyObjectFromThis().inner, target),
            ReferenceEquals(target.MyObjectFromThis(), target.MyObjectFromThis()),
            new DerivedCtorsClass().MyIntCounter(),
            new DerivedCtorsClass(1).MyIntCounter(),
            new DerivedCtorsClass(0, 0).MyIntCounter(),
            new DerivedCtorsClass(1, 0).MyIntCounter(),
            new DerivedCtorsClass(2, 0).MyIntCounter(),
            new DerivedCtorsClass("a").MyIntCounter()
        ];
    }
}
