using System.Reflection;
using HarmonyLib;
using Verse;

// ReSharper disable InconsistentNaming

namespace PurePatcher.Patches;

internal static partial class HarmonyPatches {
    internal static void AddVerboseProfiling() {
        Harmony.Patch(
            typeof(ModLister).GetMethod("RebuildModList"),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ProfilingPrefix)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ProfilingPostfix))
        );

        Harmony.Patch(
            typeof(ModMetaData).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ProfilingPrefix)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ProfilingPostfix))
        );
    }

    private static void ProfilingPrefix(object __instance, MethodBase __originalMethod) => DeepProfiler
        .Start(__originalMethod + (__instance is ModMetaData mod ? $" {mod.FolderName}" : ""));

    private static void ProfilingPostfix() => DeepProfiler.End();
}