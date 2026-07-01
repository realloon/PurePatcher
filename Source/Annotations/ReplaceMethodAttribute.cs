using JetBrains.Annotations;

// ReSharper disable UnusedParameter.Local

namespace PurePatcher.Annotations;

/// <summary>
/// Replaces the target method body with this method body.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ReplaceMethodAttribute : Attribute {
    /// <summary>
    /// Creates a method replacement annotation.
    /// </summary>
    /// <param name="targetType">The type that declares the target method.</param>
    /// <param name="targetMethodName">The name of the target method.</param>
    public ReplaceMethodAttribute(Type targetType, string targetMethodName) { }
}