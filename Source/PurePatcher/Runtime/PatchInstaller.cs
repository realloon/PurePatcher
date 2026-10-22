using System.Runtime.Loader;
using Mono.Cecil;
using PurePatcher.Process;

namespace PurePatcher.Runtime;

internal static class PatchInstaller {
    internal static void Write(AssemblySet set, PatchState state, string fingerprint) {
        var cacheFolder = state.CacheFolder(fingerprint);
        var main = set.FindAssembly(AssemblyCollector.AssemblyCSharp) ??
                   throw new InvalidOperationException("Assembly-CSharp is missing from the assembly set.");

        foreach (var assembly in set.AllAssemblies.Where(candidate => candidate.Modified && candidate != main)) {
            assembly.SerializeToByteArray();
            PatchState.WriteAtomic(
                Path.Combine(cacheFolder, ReloadRedirectTemplate.CachedFileName(assembly.SourcePath)), assembly.Bytes!);
        }

        main.SerializeToByteArray();
        Validate(main.Bytes!);

        PatchState.WriteAtomic(state.PatchedAssemblyPath(fingerprint), main.Bytes!);
        GameAssemblyInstaller.Install(state.InstalledAssemblyPath, main.Bytes!);
    }

    private static void Validate(byte[] bytes) {
        using (var module = ModuleDefinition.ReadModule(new MemoryStream(bytes))) {
            _ = module.Types.Count;
        }

        var context = new AssemblyLoadContext("PurePatcher.Validation", true);
        try {
            var assembly = context.LoadFromStream(new MemoryStream(bytes));
            assembly.GetTypes();
        } finally {
            context.Unload();
        }
    }
}