using System.Diagnostics;

namespace PurePatcher;

internal class StopwatchScope : IDisposable {
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private readonly string _title;

    private StopwatchScope(string title) {
        _title = title;
    }

    public void Dispose() {
        Logger.Info($"{_title} took {_watch.Elapsed.TotalMilliseconds}ms");
    }

    internal static StopwatchScope Measure(string title) => new(title);
}