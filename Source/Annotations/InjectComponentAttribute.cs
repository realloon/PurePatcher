namespace PurePatcher.Annotations;

/// <summary>
/// Marks an AddField accessor for automatic injection of components from target class
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InjectComponentAttribute : Attribute;