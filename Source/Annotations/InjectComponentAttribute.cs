namespace PurePatcher.Annotations;

/// <summary>
/// Marks a PurePatcherField for automatic injection of components from target class
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InjectComponentAttribute : Attribute;