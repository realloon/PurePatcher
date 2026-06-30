namespace PurePatcher.Annotations;

/// <summary>
/// Specifies a method supplying the initial value of a PurePatcherField.
/// The method has to be in the same class as the PurePatcherField.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ValueInitializerAttribute : Attribute {
    public ValueInitializerAttribute(string initializerMethod) { }
}