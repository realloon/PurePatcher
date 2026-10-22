using PurePatcher.Process;
using PurePatcher.Runtime;
using Verse;

namespace PurePatcher;

internal static class Loader {
    internal static void Reload(string packageId) {
        var state = new PatchState();
        GameAssemblyInstaller.CleanupPrevious(state.InstalledAssemblyPath);
        state.SyncVanillaBaseline();

        var fingerprint = PatchFingerprint.Compute(state);

        if (AssemblyInjection.TryReadFingerprint(typeof(Game).Assembly) == fingerprint) {
            Logger.Verbose($"Assembly-CSharp is patched for {fingerprint}");
            state.ClearAttempts(fingerprint);
            return;
        }

        if (state.IsBlocked(fingerprint)) {
            Logger.Error($"PurePatcher gave up after repeated boot failures ({fingerprint}). " +
                         "Verify the game files in Steam to retry.");
            return;
        }

        try {
            state.CleanupOtherOutputs(fingerprint);

            var set = AssemblyCollector.Collect(state);
            using (StopwatchScope.Measure("Game processing")) {
                GameProcessing.Process(set, state, fingerprint, packageId);
            }

            PatchInstaller.Write(set, state, fingerprint);

            Logger.Info($"Patched Assembly-CSharp ({fingerprint}); restarting the game.");
            GameRestart.Restart();
        } catch (Exception e) {
            Logger.Error($"Failed to patch the game, continuing unpatched: {e}");
        }
    }
}