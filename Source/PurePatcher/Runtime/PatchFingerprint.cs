using System.Text;
using RimWorld;
using Verse;

namespace PurePatcher.Runtime;

internal static class PatchFingerprint {
    internal static string Compute(PatchState state) {
        var builder = new StringBuilder();
        builder.Append(VersionControl.CurrentVersionString).Append('\n');
        builder.Append(AppContext.BaseDirectory).Append('\n');
        builder.Append(state.VanillaHash()).Append('\n');

        var self = typeof(PurePatcherMod).Assembly;
        builder.Append(self.GetName().Version).Append('\n');
        builder.Append(self.ManifestModule.ModuleVersionId).Append('\n');

        foreach (var mod in LoadedModManager.RunningModsListForReading) {
            builder.Append(mod.PackageId).Append('\n');

            foreach (var (relativePath, file) in ModAssemblyFiles(mod)) {
                builder.Append(relativePath).Append('|')
                    .Append(file.Length).Append('|')
                    .Append(file.LastWriteTimeUtc.Ticks).Append('\n');
            }
        }

        return PatchState.Hash(Encoding.UTF8.GetBytes(builder.ToString()))[..16];
    }

    // Must list the mod's assemblies as they exist on disk: in a patched session the loaded ones
    // come from the cache and would change the fingerprint.
    private static IEnumerable<(string, FileInfo)> ModAssemblyFiles(ModContentPack mod) =>
        ModContentPack.GetAllFilesForModPreserveOrder(mod, "Assemblies/",
                extension => extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(entry => (entry.Item1, entry.Item2));
}