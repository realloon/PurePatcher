namespace PurePatcher.Process;

internal static class GameProcessing {
    internal static void Process(AssemblySet set) {
        var asmCSharp = set.FindAssembly(AssemblyCollector.AssemblyCSharp)!;
        var prepatcherActive = PrepatcherCompatibility.IsActive(); // [Prepatcher] compatibility

        // Other code assumes that these always get reloaded
        asmCSharp.SetNeedsReload();
        set.FindAssembly("0Harmony")!.SetNeedsReload();

        var monoModUtils = set.FindAssembly("MonoMod.Utils");
        monoModUtils?.SetNeedsReload();

        // Field addition
        var fieldAdder = new FieldAdder(set);
        GameInjections.RegisterInjections(fieldAdder);
        fieldAdder.ProcessAllAssemblies();

        // Method replacement
        MethodReplacer.RunReplacements(set, AssemblyCollector.ActivePackageIds());

        if (prepatcherActive) {
            Logger.Info("Prepatcher is active; skipping duplicate startup support patches.");
        } else {
            // Fix the update order of RimWorld's reloaded Unity components
            ExecutionOrderFixer.ApplyExecutionOrderAttributes(asmCSharp.ModuleDefinition);
            asmCSharp.Modified = true; // Mark as modified so it's serialized and new attributes are applied
        }

        // Assembly rewriting
        AssemblyRewriter.RunRewrites(set, AssemblyCollector.AssemblyCSharp,
            skippedRewriterType: prepatcherActive ? PrepatcherCompatibility.BuiltinRewriteTypeToSkip : null);
    }
}