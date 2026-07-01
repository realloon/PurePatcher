namespace PurePatcher.Annotations;

/// <summary>
/// Specifies a method supplying the initial value of an AddField accessor.
/// The method has to be in the same class as the AddField accessor.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InitValueAttribute : Attribute {
    /// <summary>
    /// Creates an initializer annotation for an AddField accessor.
    /// </summary>
    /// <param name="initializerMethod">The name of the method that supplies the initial field value.</param>
    // ReSharper disable once UnusedParameter.Local
    public InitValueAttribute(string initializerMethod) { }
}