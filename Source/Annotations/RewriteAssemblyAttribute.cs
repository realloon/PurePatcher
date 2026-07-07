using JetBrains.Annotations;

namespace PurePatcher.Annotations;

/// <summary>
/// Marks a method that rewrites the target assembly module.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class RewriteAssemblyAttribute : Attribute;