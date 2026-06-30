using JetBrains.Annotations;

namespace PurePatcher.Annotations;

/// <summary>
/// Marks an assembly rewriting method
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class FreePatchAllAttribute : Attribute;