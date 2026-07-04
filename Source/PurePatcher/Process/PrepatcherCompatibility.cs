using PurePatcher.Patches;
using Verse;

namespace PurePatcher.Process;

internal static class PrepatcherCompatibility {
    private const string PackageId = "zetrith.prepatcher";

    internal static bool IsActive() => ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) != null;

    internal static readonly HashSet<Type> BuiltinFreePatchTypes = [
        typeof(AssemblyLoadingFreePatch),
        typeof(WorldCameraFreePatch)
    ];
}