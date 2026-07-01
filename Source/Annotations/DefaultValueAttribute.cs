namespace PurePatcher.Annotations;

/// <summary>
/// Specifies the default value of an AddField accessor.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DefaultValueAttribute : Attribute {
    /// <summary>
    /// Creates a default value annotation for an AddField accessor.
    /// </summary>
    /// <param name="defaultValue">The value assigned to the generated field during construction.</param>
    // ReSharper disable once UnusedParameter.Local
    public DefaultValueAttribute(object? defaultValue) { }
}