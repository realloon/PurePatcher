using System.Collections.Generic;

namespace PurePatcher.Process;

public static class InjectionHelper {
    public static void Clear<T, TF>(ref TF? field, object target) {
        if (target is not T) return;

        field = default;
    }

    public static void TryInject<T, TF>(ref TF field, object target, IEnumerable<object> comps) {
        if (target is not T) return;
        if (field != null) return;

        foreach (var comp in comps) {
            if (comp is not TF casted) continue;

            field = casted;
            break;
        }
    }
}