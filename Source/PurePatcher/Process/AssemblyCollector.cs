using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace PurePatcher.Process;

internal static class AssemblyCollector {
    internal const string AssemblyCSharp = "Assembly-CSharp";

    internal static IEnumerable<(string, string)> SystemAssemblyPaths() => Directory
        .GetFiles(Path.Combine(Application.dataPath, Util.ManagedFolderOS()), "*.dll")
        .Select(asmPath => ($"(System) {Path.GetFileName(asmPath)}", asmPath)); // Collect System and Unity assemblies

    internal static IEnumerable<(string, Assembly)> ModAssemblies() {
        foreach (var (mod, modAssembly) in GetModAssemblies()) {
            var name = modAssembly.GetName().Name;
            yield return ($"(mod {mod.PackageIdPlayerFacing}) {name}", modAssembly);
        }
    }

    private static IEnumerable<(ModContentPack, Assembly)> GetModAssemblies() => LoadedModManager
        .RunningModsListForReading
        .SelectMany(m => m.assemblies.loadedAssemblies, (m, a) => (m, a));
}
