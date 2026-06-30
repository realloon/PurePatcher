using System.Diagnostics;

namespace PurePatcher;

internal class StopwatchScope : IDisposable {
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private string title;

    private StopwatchScope() { }

    public void Dispose() {
        Lg.Info($"{title} took {watch.Elapsed.TotalMilliseconds}ms");
    }

    internal static StopwatchScope Measure(string title) => new() {
        title = title
    };
}