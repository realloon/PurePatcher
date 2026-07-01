namespace PurePatcher.Annotations;

/// <summary>
/// Marks an AddField accessor for automatic binding to a component from the target class.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BindComponentAttribute : Attribute;