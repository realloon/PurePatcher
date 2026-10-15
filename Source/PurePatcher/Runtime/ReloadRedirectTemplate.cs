using System.Reflection;
using System.Runtime.Loader;

namespace PurePatcher.Runtime;

// These bodies are transplanted into the patched Assembly-CSharp, so they must stay self-contained.
internal static class ReloadRedirectTemplate {
    internal const string CacheFolderPlaceholder = "PurePatcher.CacheFolderPlaceholder";

    internal static Dictionary<string, Assembly>? Loaded;

    internal static Assembly LoadFrom(string filePath) {
        var cached = Path.Combine(CacheFolderPlaceholder, CachedFileName(filePath));
        if (!File.Exists(cached)) {
            // Untouched mods load exactly like vanilla: the runtime already resolves duplicates by name.
            if (Loaded is not { Count: > 0 }) return Assembly.LoadFrom(filePath);

            var existingName = AssemblyName.GetAssemblyName(filePath).Name;
            return existingName != null && Loaded.TryGetValue(existingName, out var existing)
                ? existing
                : Assembly.LoadFrom(filePath);
        }

        var loaded = LoadedAssemblies();
        var name = AssemblyName.GetAssemblyName(filePath).Name!;
        if (loaded.TryGetValue(name, out var alreadyLoaded)) return alreadyLoaded;

        var assembly = Assembly.Load(File.ReadAllBytes(cached));
        loaded[name] = assembly;
        return assembly;
    }

    internal static Dictionary<string, Assembly> LoadedAssemblies() {
        if (Loaded != null) return Loaded;

        var map = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        foreach (var context in AssemblyLoadContext.All) {
            foreach (var assembly in context.Assemblies) {
                var name = assembly.GetName().Name;
                if (name != null && !map.ContainsKey(name)) {
                    map[name] = assembly;
                }
            }
        }

        return Loaded = map;
    }

    // Two mods can ship DLLs with the same file name, so the cache needs the full path in the name.
    internal static string CachedFileName(string filePath) {
        var hash = 2166136261u;
        foreach (var character in filePath) {
            hash = (hash ^ character) * 16777619u;
        }

        return Path.GetFileNameWithoutExtension(filePath) + "." + hash.ToString("x8") + ".dll";
    }
}
