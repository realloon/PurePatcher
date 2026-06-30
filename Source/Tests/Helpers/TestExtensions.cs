using System.Collections.Generic;

namespace Tests.Helpers;

public static class TestExtensions {
    internal static IEnumerable<T> EnumerableOf<T>(T obj) {
        yield return obj;
    }
}
