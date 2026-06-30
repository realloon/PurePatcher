using PurePatcher.Annotations;
using TestAssemblyTarget;

namespace Tests;

public static class DefaultValues {
    // bool defaults
    [PurePatcherField]
    [DefaultValue(false)]
    public static extern ref bool MyBoolDefaultFalse(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(true)]
    public static extern ref bool MyBoolDefaultTrue(this TargetClass target);

    // int defaults
    [PurePatcherField]
    [DefaultValue(int.MinValue)]
    public static extern ref int MyIntDefaultMin(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(int.MaxValue)]
    public static extern ref int MyIntDefaultMax(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref int MyIntDefaultNull(this TargetClass target);

    // uint defaults
    [PurePatcherField]
    [DefaultValue(uint.MinValue)]
    public static extern ref uint MyUIntDefaultMin(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(uint.MaxValue)]
    public static extern ref uint MyUIntDefaultMax(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref uint MyUIntDefaultNull(this TargetClass target);

    // long defaults
    [PurePatcherField]
    [DefaultValue(long.MinValue)]
    public static extern ref long MyLongDefaultMin(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(long.MaxValue)]
    public static extern ref long MyLongDefaultMax(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref long MyLongDefaultNull(this TargetClass target);

    // ulong defaults
    [PurePatcherField]
    [DefaultValue(ulong.MinValue)]
    public static extern ref ulong MyULongDefaultMin(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(ulong.MaxValue)]
    public static extern ref ulong MyULongDefaultMax(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref ulong MyULongDefaultNull(this TargetClass target);

    // float defaults
    [PurePatcherField]
    [DefaultValue(float.MinValue)]
    public static extern ref float MyFloatDefaultMin(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(float.MaxValue)]
    public static extern ref float MyFloatDefaultMax(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref float MyFloatDefaultNull(this TargetClass target);

    // double defaults
    [PurePatcherField]
    [DefaultValue(double.MinValue)]
    public static extern ref double MyDoubleDefaultMin(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(double.MaxValue)]
    public static extern ref double MyDoubleDefaultMax(this TargetClass target);

    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref double MyDoubleDefaultNull(this TargetClass target);

    // string defaults
    [PurePatcherField]
    [DefaultValue(null)]
    public static extern ref string MyStringDefaultNull(this TargetClass target);

    [PurePatcherField]
    [DefaultValue("a")]
    public static extern ref string MyStringDefault(this TargetClass target);

    // Value initializers
    [PurePatcherField]
    [ValueInitializer(nameof(IntParameterlessInitializer))]
    public static extern ref int MyIntParameterless(this TargetClass target);

    [PurePatcherField]
    [ValueInitializer(nameof(IntThisInitializer))]
    public static extern ref int MyIntFromThis(this TargetClass target);

    [PurePatcherField]
    [ValueInitializer(nameof(ObjectThisInitializer))]
    public static extern ref SecondTargetClass MyObjectFromThis(this TargetClass target);

    [PurePatcherField]
    [ValueInitializer(nameof(CounterInitializer))]
    public static extern ref int MyIntCounter(this DerivedCtorsClass target);

    public static int IntParameterlessInitializer() => 1;

    public static int IntThisInitializer(TargetClass? obj) => obj != null ? 1 : -1;

    public static SecondTargetClass ObjectThisInitializer(TargetClass obj) => new(obj);

    public static int CounterInitializer(DerivedCtorsClass ctors) => ++ctors.counter;
}