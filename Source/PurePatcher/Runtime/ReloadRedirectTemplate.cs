using System.Reflection;
using System.Runtime.Loader;

namespace PurePatcher.Runtime;

// These bodies are transplanted into the patched Assembly-CSharp, so they must stay self-contained.
internal static class ReloadRedirectTemplate {
    internal const string CacheFolderPlaceholder = "PurePatcher.CacheFolderPlaceholder";

    internal static Assembly LoadFrom(string filePath) {
        var assemblyName = AssemblyName.GetAssemblyName(filePath).Name;
        foreach (var context in AssemblyLoadContext.All) {
            foreach (var loaded in context.Assemblies) {
                if (loaded.GetName().Name == assemblyName) {
                    return loaded;
                }
            }
        }

        var cached = Path.Combine(CacheFolderPlaceholder, CachedFileName(filePath));
        return File.Exists(cached) ? Assembly.Load(File.ReadAllBytes(cached)) : Assembly.LoadFrom(filePath);
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