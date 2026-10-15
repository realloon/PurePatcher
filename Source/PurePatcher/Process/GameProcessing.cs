using PurePatcher.Runtime;
using Verse;

namespace PurePatcher.Process;

internal static class GameProcessing {
    internal static void Process(AssemblySet set, PatchState state, string fingerprint, string packageId) {
        var asmCSharp = set.FindAssembly(AssemblyCollector.AssemblyCSharp)!;

        // Field addition
        var fieldAdder = new FieldAdder(set);
        GameInjections.RegisterInjections(fieldAdder);
        fieldAdder.ProcessAllAssemblies();

        // Method replacement
        MethodReplacer.RunReplacements(set, AssemblyCollector.ActivePackageIds());

        // Support code the patched Assembly-CSharp needs
        AssemblyInjection.InjectRedirect(asmCSharp.ModuleDefinition, state.CacheFolder(fingerprint));
        AssemblyInjection.InjectMarker(asmCSharp.ModuleDefinition, fingerprint);
        AssemblyInjection.InjectBootGuard(asmCSharp.ModuleDefinition, state.Folder, fingerprint,
            GenFilePaths.ModsConfigFilePath, packageId);
        // Assembly rewriting
        AssemblyRewriter.RunRewrites(set, AssemblyCollector.AssemblyCSharp);

        asmCSharp.Modified = true;

        // Last, once every rewrite has landed: exemptions for each referenced assembly.
        foreach (var modified in set.AllAssemblies.Where(assembly => assembly.Modified)) {
            AssemblyInjection.InjectAccessCheckExemptions(modified.ModuleDefinition);
        }
    }
}