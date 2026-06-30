namespace PurePatcher.Annotations;

/// <summary>
/// Specifies the default value of a PurePatcherField
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DefaultValueAttribute : Attribute {
    public DefaultValueAttribute(object? defaultValue) { }
}