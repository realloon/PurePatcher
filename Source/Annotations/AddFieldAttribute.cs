using JetBrains.Annotations;

namespace PurePatcher.Annotations;

/// <summary>
/// Marks the accessor of a requested new field
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class AddFieldAttribute : Attribute;