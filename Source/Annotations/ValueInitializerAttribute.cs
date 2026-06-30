namespace PurePatcher.Annotations;

/// <summary>
/// Specifies a method supplying the initial value of a PurePatcherField.
/// The method has to be in the same class as the PurePatcherField.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ValueInitializerAttribute : Attribute {
    /// <summary>
    /// Creates an initializer annotation for a PurePatcherField.
    /// </summary>
    /// <param name="initializerMethod">The name of the method that supplies the initial field value.</param>
    // ReSharper disable once UnusedParameter.Local
    public ValueInitializerAttribute(string initializerMethod) { }
}