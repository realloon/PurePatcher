using System.Reflection;

namespace PurePatcher.Runtime;

// This body is transplanted into the patched Assembly-CSharp, so it must stay self-contained.
internal static class BootGuardTemplate {
    internal const string StateFolderPlaceholder = "PurePatcher.StateFolderPlaceholder";
    internal const string FingerprintPlaceholder = "PurePatcher.FingerprintPlaceholder";
    internal const string ModsConfigPathPlaceholder = "PurePatcher.ModsConfigPathPlaceholder";
    internal const string PackageIdPlaceholder = "PurePatcher.PackageIdPlaceholder";

    internal static void Check() {
        try {
            var vanilla = Path.Combine(StateFolderPlaceholder, "vanilla", "Assembly-CSharp.dll");
            var attemptsPath = Path.Combine(StateFolderPlaceholder, "attempts-" + FingerprintPlaceholder + ".txt");
            var installed = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Assembly-CSharp.dll");

            var hasVanilla = File.Exists(vanilla);
            var modsConfig = File.Exists(ModsConfigPathPlaceholder)
                ? File.ReadAllText(ModsConfigPathPlaceholder)
                : null;

            // Uninstalled or disabled: the patched assembly still refers to other mods.
            if (hasVanilla && modsConfig != null &&
                !modsConfig.Contains(PackageIdPlaceholder, StringComparison.OrdinalIgnoreCase)) {
                File.Delete(attemptsPath);
                File.Copy(vanilla, installed, true);
                Environment.Exit(7);
                return;
            }

            var attempts = File.Exists(attemptsPath) ? int.Parse(File.ReadAllText(attemptsPath)) : 0;
            attempts++;
            File.WriteAllText(attemptsPath, attempts.ToString());
            if (attempts <= 3 || !hasVanilla) return;

            File.Copy(vanilla, installed, true);

            // Keep the block alive until the game file is replaced again (Steam verification).
            File.SetLastWriteTimeUtc(attemptsPath, File.GetLastWriteTimeUtc(installed).AddSeconds(1));
            Environment.Exit(7);
        } catch {
            // A failing guard must never break the boot.
        }
    }
}