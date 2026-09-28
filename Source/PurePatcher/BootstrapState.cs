namespace PurePatcher;

internal static class BootstrapState {
    private const string StartedOnceKey = "PurePatcher.StartedOnce";

    internal static bool StartedOnce => AppDomain.CurrentDomain.GetData(StartedOnceKey) switch {
        null => false,
        bool startedOnce => startedOnce,
        _ => throw new InvalidOperationException($"{StartedOnceKey} has invalid value.")
    };

    internal static void MarkStartedOnce() {
        AppDomain.CurrentDomain.SetData(StartedOnceKey, true);
    }
}