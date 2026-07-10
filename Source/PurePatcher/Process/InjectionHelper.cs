namespace PurePatcher.Process;

public static class InjectionHelper {
    public static void Clear<T, TF>(ref TF? field, object target) {
        if (target is not T) return;

        field = default;
    }
}