using System.Reflection;
using PurePatcher.Runtime;
using Verse;

namespace PurePatcher.Process;

internal static class AssemblyCollector {
    internal const string AssemblyCSharp = "Assembly-CSharp";
    private const string AnnotationsAssembly = "PurePatcher.Annotations";

    // 1.7 is self-contained: the runtime and game assemblies live next to the executable.
    internal static IEnumerable<(string, string)> SystemAssemblyPaths() => Directory
        .GetFiles(AppContext.BaseDirectory, "*.dll")
        .Where(ManagedAssemblyFiles.IsAssembly)
        .Select(asmPath => ($"(System) {Path.GetFileName(asmPath)}", asmPath));

    internal static AssemblySet Collect(PatchState state) {
        var set = new AssemblySet();

        // Always rewrite the pristine file: the installed one may already carry a patch.
        set.AddAssembly(AssemblyCSharp, state.VanillaAssemblyPath, null);

        foreach (var (friendlyName, path) in SystemAssemblyPaths()) {
            if (Path.GetFileName(path).Equals($"{AssemblyCSharp}.dll", StringComparison.OrdinalIgnoreCase)) continue;

            set.AddAssembly(friendlyName, path, null).AllowPatches = false;
        }

        foreach (var mod in LoadedModManager.RunningModsListForReading) {
            var files = ModAssemblyFiles(mod);

            foreach (var assembly in mod.assemblies.loadedAssemblies) {
                var name = assembly.GetName().Name;

                // Don't add system assemblies packaged by mods
                if (name == null || set.HasAssembly(name)) continue;

                // A patched session loads mod assemblies from the cache, where Location is empty.
                var path = files.FirstOrDefault(file => AssemblyName.GetAssemblyName(file.FullName).Name == name)
                    ?.FullName;
                if (path == null) {
                    Logger.Verbose($"(mod {mod.PackageIdPlayerFacing}) {name} has no file on disk, skipping");
                    continue;
                }

                var added = set.AddAssembly($"(mod {mod.PackageIdPlayerFacing}) {name}", path, null);
                added.SourceAssembly = assembly;
                added.ProcessAttributes = ReferencesAnnotations(added.ModuleDefinition);
            }
        }

        return set;
    }

    internal static HashSet<string> ActivePackageIds() {
        var packageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in LoadedModManager.RunningModsListForReading) {
            packageIds.Add(mod.PackageId);
        }

        return packageIds;
    }

    private static List<FileInfo> ModAssemblyFiles(ModContentPack mod) => ModContentPack
        .GetAllFilesForModPreserveOrder(mod, "Assemblies/",
            extension => extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        .Select(entry => entry.Item2)
        .ToList();

    private static bool ReferencesAnnotations(Mono.Cecil.ModuleDefinition module) => module
        .AssemblyReferences.Any(reference => reference.Name == AnnotationsAssembly);
}