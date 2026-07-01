using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class DefaultValues {
    // bool defaults
    extension(TargetClass target) {
        [AddField]
        [DefaultValue(false)]
        public extern ref bool MyBoolDefaultFalse();

        [AddField]
        [DefaultValue(true)]
        public extern ref bool MyBoolDefaultTrue();

        [AddField]
        [DefaultValue(int.MinValue)]
        public extern ref int MyIntDefaultMin();

        [AddField]
        [DefaultValue(int.MaxValue)]
        public extern ref int MyIntDefaultMax();

        [AddField]
        [DefaultValue(null)]
        public extern ref int MyIntDefaultNull();

        [AddField]
        [DefaultValue(uint.MinValue)]
        public extern ref uint MyUIntDefaultMin();

        [AddField]
        [DefaultValue(uint.MaxValue)]
        public extern ref uint MyUIntDefaultMax();

        [AddField]
        [DefaultValue(null)]
        public extern ref uint MyUIntDefaultNull();

        [AddField]
        [DefaultValue(long.MinValue)]
        public extern ref long MyLongDefaultMin();

        [AddField]
        [DefaultValue(long.MaxValue)]
        public extern ref long MyLongDefaultMax();

        [AddField]
        [DefaultValue(null)]
        public extern ref long MyLongDefaultNull();

        [AddField]
        [DefaultValue(ulong.MinValue)]
        public extern ref ulong MyULongDefaultMin();

        [AddField]
        [DefaultValue(ulong.MaxValue)]
        public extern ref ulong MyULongDefaultMax();

        [AddField]
        [DefaultValue(null)]
        public extern ref ulong MyULongDefaultNull();

        [AddField]
        [DefaultValue(float.MinValue)]
        public extern ref float MyFloatDefaultMin();

        [AddField]
        [DefaultValue(float.MaxValue)]
        public extern ref float MyFloatDefaultMax();

        [AddField]
        [DefaultValue(null)]
        public extern ref float MyFloatDefaultNull();

        [AddField]
        [DefaultValue(double.MinValue)]
        public extern ref double MyDoubleDefaultMin();

        [AddField]
        [DefaultValue(double.MaxValue)]
        public extern ref double MyDoubleDefaultMax();

        [AddField]
        [DefaultValue(null)]
        public extern ref double MyDoubleDefaultNull();

        [AddField]
        [DefaultValue(null)]
        public extern ref string MyStringDefaultNull();

        [AddField]
        [DefaultValue("a")]
        public extern ref string MyStringDefault();

        [AddField]
        [InitValue(nameof(IntParameterlessInitializer))]
        public extern ref int MyIntParameterless();

        [AddField]
        [InitValue(nameof(IntThisInitializer))]
        public extern ref int MyIntFromThis();

        [AddField]
        [InitValue(nameof(ObjectThisInitializer))]
        public extern ref SecondTargetClass MyObjectFromThis();
    }

    [AddField]
    [InitValue(nameof(CounterInitializer))]
    public static extern ref int MyIntCounter(this DerivedCtorsClass target);

    public static int IntParameterlessInitializer() => 1;

    public static int IntThisInitializer(TargetClass? obj) => obj != null ? 1 : -1;

    public static SecondTargetClass ObjectThisInitializer(TargetClass obj) => new(obj);

    public static int CounterInitializer(DerivedCtorsClass ctors) => ++ctors.Counter;

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

    public static object?[] TestDefaultInitValues() {
        var target = new TargetClass();
        return [
            target.MyIntParameterless(),
            target.MyIntFromThis(),
            ReferenceEquals(target.MyObjectFromThis().Inner, target),
            // ReSharper disable once EqualExpressionComparison
            // Must call twice to verify the initializer stores and reuses the same instance.
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