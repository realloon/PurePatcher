namespace PurePatcher.Runtime;

internal static class GameAssemblyInstaller {
    internal static void Install(string path, byte[] bytes) {
        // Windows permits renaming an image mapped by CoreCLR but forbids deleting
        // that image. Move(overwrite:true) deletes the destination and therefore fails.
        // Keep the old mapping alive under a unique name until the game restarts.
        if (!OperatingSystem.IsWindows()) {
            PatchState.WriteAtomic(path, bytes);
            return;
        }

        CleanupPrevious(path);
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, bytes);
        var previous = path + ".purepatcher-" + Guid.NewGuid().ToString("N") + ".previous";
        File.Move(path, previous);
        try {
            File.Move(temp, path);
        } catch (Exception installError) {
            try {
                File.Move(previous, path);
            } catch (Exception restoreError) {
                throw new AggregateException("Could not install or restore the game assembly", installError, restoreError);
            }
            throw;
        }
        TryDelete(previous);
    }

    internal static void CleanupPrevious(string path) {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var pattern = Path.GetFileName(path) + ".purepatcher-*.previous";
        foreach (var previous in Directory.EnumerateFiles(folder, pattern)) TryDelete(previous);
    }

    private static void TryDelete(string path) {
        try {
            File.Delete(path);
        } catch (UnauthorizedAccessException) {
            // The old image is still mapped. A later boot will clean it up.
        } catch (IOException) {
            // The preceding process may still be shutting down.
        }
    }
}
