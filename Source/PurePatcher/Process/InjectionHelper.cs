namespace PurePatcher.Process;

public static class InjectionHelper {
    public static void TryInject<T>(ref T? field, System.Collections.IList? comps) {
        if (field != null) return;
        if (comps == null) return;

        foreach (var comp in comps) {
            if (comp is not T casted) continue;

            field = casted;
            break;
        }
    }
}