using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using PurePatcher.Annotations;
using ICustomAttributeProvider = System.Reflection.ICustomAttributeProvider;

namespace PurePatcher.Process;

internal static class AssemblyRewriter {
    internal static void RunRewrites(AssemblySet assemblySet, string mainAssemblyName,
        Action<ModifiableAssembly>? callback = null,
        Type? skippedRewriterType = null) { // [Prepatcher] compatibility
        Logger.Verbose("Running assembly rewrites");

        var rewriterAssemblies = assemblySet.AllAssemblies
            .Where(a => a.ProcessAttributes && a.SourceAssembly != null);

        var mainAssembly = assemblySet.FindAssembly(mainAssemblyName);
        if (mainAssembly == null) {
            throw new Exception($"Couldn't find main assembly {mainAssemblyName} in the assembly set");
        }

        foreach (var modifiableAssembly in rewriterAssemblies)
        foreach (var rewriter in FindAllAssemblyRewrites(modifiableAssembly.SourceAssembly!)) {
            if (rewriter.DeclaringType == skippedRewriterType) {
                Logger.Info("Skipping builtin assembly rewrite.");
                continue;
            }

            callback?.Invoke(modifiableAssembly);
            Logger.Verbose($"Running assembly rewrite: {rewriter.FullDescription()}");

            if (InvokeRewriter(rewriter, mainAssembly.ModuleDefinition)) {
                mainAssembly.Modified = true;
            }
        }
    }

    private static bool InvokeRewriter(MethodInfo rewriter, ModuleDefinition moduleToRewrite) {
        try {
            var ret = rewriter.Invoke(null, [moduleToRewrite]);
            return ret == null || (bool)ret;
        } catch (Exception e) {
            Logger.Error($"Exception running assembly rewrite {rewriter.FullDescription()}: {e}");
            return false;
        }
    }

    private static bool IsDefinedSafe<T>(ICustomAttributeProvider provider) where T : Attribute {
        try {
            return provider.IsDefined(typeof(T), false);
        } catch (FileNotFoundException) {
            return false;
        } catch (TypeLoadException) {
            return false;
        } catch (MissingMethodException) {
            return false;
        }
    }

    private static IEnumerable<MethodInfo> FindAllAssemblyRewrites(Assembly rewriterAssembly) => rewriterAssembly
        .GetTypes()
        .Where(AccessTools.IsStatic)
        .SelectMany(AccessTools.GetDeclaredMethods, (type, m) => new { type, m })
        .Where(t => IsDefinedSafe<RewriteAssemblyAttribute>(t.m))
        .Select(t => t.m);
}