using Verse;

namespace PurePatcher.Runtime;

internal sealed class PatchState {
    internal PatchState() {
        Folder = Path.Combine(GenFilePaths.SaveDataFolderPath, "PurePatcher");
        Directory.CreateDirectory(Folder);
    }

    internal string Folder { get; }

    internal string InstalledAssemblyPath => Path.Combine(AppContext.BaseDirectory, "Assembly-CSharp.dll");

    internal string VanillaAssemblyPath => Path.Combine(Folder, "vanilla", "Assembly-CSharp.dll");

    internal string CacheFolder(string fingerprint) => Path.Combine(Folder, "cache", fingerprint);

    internal string PatchedAssemblyPath(string fingerprint) =>
        Path.Combine(Folder, "patched", $"Assembly-CSharp.{fingerprint}.dll");

    private string AttemptsPath(string fingerprint) => Path.Combine(Folder, $"attempts-{fingerprint}.txt");

    // The guard in the patched assembly restores the vanilla file once it sees more than three boots.
    internal bool IsBlocked(string fingerprint) {
        if (!File.Exists(AttemptsPath(fingerprint))) return false;
        if (!int.TryParse(File.ReadAllText(AttemptsPath(fingerprint)), out var attempts) || attempts <= 3) {
            return false;
        }

        // A new install of the game file (Steam verification, game update) clears the block.
        if (File.GetLastWriteTimeUtc(InstalledAssemblyPath) > File.GetLastWriteTimeUtc(AttemptsPath(fingerprint))) {
            ClearAttempts(fingerprint);
            return false;
        }

        return true;
    }

    internal void ClearAttempts(string fingerprint) {
        try {
            File.Delete(AttemptsPath(fingerprint));
        } catch (IOException) { }
    }

    // Keeps the baseline in step with the installed game file: fills it in on the first patch,
    // and picks up the new one after a game update or a Steam file verification.
    internal void SyncVanillaBaseline() {
        if (AssemblyInjection.TryReadFingerprintFromFile(InstalledAssemblyPath) != null) {
            if (!File.Exists(VanillaAssemblyPath)) {
                throw new InvalidOperationException(
                    "The installed Assembly-CSharp is patched but the vanilla baseline is missing.");
            }

            return;
        }

        var installed = File.ReadAllBytes(InstalledAssemblyPath);
        if (File.Exists(VanillaAssemblyPath) && Hash(installed) == Hash(File.ReadAllBytes(VanillaAssemblyPath))) {
            return;
        }

        WriteAtomic(VanillaAssemblyPath, installed);
    }

    internal void CleanupOtherOutputs(string fingerprint) {
        DeleteOtherEntries(Path.Combine(Folder, "cache"), fingerprint);
        DeleteOtherEntries(Path.Combine(Folder, "patched"), fingerprint);

        foreach (var attempts in Directory.EnumerateFiles(Folder, "attempts-*.txt")) {
            var name = Path.GetFileNameWithoutExtension(attempts)["attempts-".Length..];
            if (name != fingerprint) File.Delete(attempts);
        }
    }

    internal string VanillaHash() => File.Exists(VanillaAssemblyPath)
        ? Hash(File.ReadAllBytes(VanillaAssemblyPath))
        : Hash(File.ReadAllBytes(InstalledAssemblyPath));

    internal static string Hash(byte[] bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));

    internal static void WriteAtomic(string path, byte[] bytes) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, bytes);
        File.Move(temp, path, true);
    }

    private static void DeleteOtherEntries(string folder, string fingerprint) {
        if (!Directory.Exists(folder)) return;

        foreach (var entry in Directory.EnumerateFileSystemEntries(folder)) {
            if (Path.GetFileName(entry).Contains(fingerprint, StringComparison.Ordinal)) continue;

            if (Directory.Exists(entry)) {
                Directory.Delete(entry, true);
            } else {
                File.Delete(entry);
            }
        }
    }
}