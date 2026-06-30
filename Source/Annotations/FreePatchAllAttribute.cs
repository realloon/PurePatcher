namespace PurePatcher.Annotations;

/// <summary>
/// Marks an assembly rewriting method
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FreePatchAllAttribute : Attribute;