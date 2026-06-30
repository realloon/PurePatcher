namespace PurePatcher;

internal static class Logger {
    internal static Action<string>? InfoFunc;
    internal static Action<string>? ErrorFunc;
    internal static Action<string>? VerboseFunc;

    internal static void Info(string msg) => InfoFunc?.Invoke(msg);

    internal static void Error(string msg) => ErrorFunc?.Invoke(msg);

    internal static void Verbose(string msg) => VerboseFunc?.Invoke(msg);
}