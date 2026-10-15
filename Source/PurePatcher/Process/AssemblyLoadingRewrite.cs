using System.Reflection;
using Mono.Cecil;
using MonoMod.Utils;
using PurePatcher.Annotations;
using PurePatcher.Runtime;

namespace PurePatcher.Process;

public static class AssemblyLoadingRewrite {
    [RewriteAssembly]
    public static void ReplaceAssemblyLoading(ModuleDefinition module) {
        var type = module.GetType($"{nameof(Verse)}.{nameof(Verse.ModAssemblyHandler)}");
        var method = type.FindMethod(nameof(Verse.ModAssemblyHandler.ReloadAll)) ??
                     throw new MissingMethodException(type.FullName, nameof(Verse.ModAssemblyHandler.ReloadAll));

        var redirect = module.GetType($"{AssemblyInjection.Namespace}.{AssemblyInjection.ReloadRedirectType}")
                           ?.Methods.FirstOrDefault(candidate => candidate.Name == "LoadFrom") ??
                       throw new MissingMethodException(AssemblyInjection.ReloadRedirectType, "LoadFrom");

        foreach (var inst in method.Body.Instructions) {
            if (inst.Operand is MethodReference { Name: nameof(Assembly.LoadFrom) }) {
                inst.Operand = redirect;
            }
        }
    }
}