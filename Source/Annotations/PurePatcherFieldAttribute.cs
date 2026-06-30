namespace PurePatcher.Annotations;

/// <summary>
/// Marks the accessor of a requested new field
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class PurePatcherFieldAttribute : Attribute;