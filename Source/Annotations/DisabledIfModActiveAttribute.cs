namespace PurePatcher.Annotations;

/// <summary>
/// Disables a ReplaceMethod patch when any listed RimWorld mod packageId is active.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DisabledIfModActiveAttribute : Attribute {
    /// <summary>
    /// Creates a mod-activity condition for a ReplaceMethod patch.
    /// </summary>
    /// <param name="packageIds">The RimWorld mod packageIds that disable the patch when active.</param>
    // ReSharper disable once UnusedParameter.Local
    public DisabledIfModActiveAttribute(params string[] packageIds) { }
}