using HarmonyLib;

namespace PurePatcher.Patches;

internal static partial class HarmonyPatches {
    private static readonly Harmony Harmony = new("purepatcher");
}