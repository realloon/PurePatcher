using System.Linq;
using HarmonyLib;
using Verse;

namespace PurePatcher;

internal static partial class HarmonyPatches {
    internal static void PatchModLoading() {
        Lg.Verbose("Patching mod loading");

        // If a mod needs to loadAfter brrainz.harmony, then also loadAfter vortex.purepatcher
        harmony.Patch(
            typeof(ModMetaData.ModMetaDataInternal).GetMethod("InitVersionedData"),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(InitVersionedDataPostfix))
        );

        // Let PurePatcher satisfy modDependencies on brrainz.harmony
        harmony.Patch(
            typeof(ModDependency).GetProperty("IsSatisfied")!.GetGetMethod(),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(IsSatisfiedPostfix))
        );

        // Fixup already loaded mods
        foreach (var modMeta in ModLister.AllInstalledMods.Select(m => m.meta))
            InitVersionedDataPostfix(modMeta);
    }

    private static void InitVersionedDataPostfix(ModMetaData.ModMetaDataInternal __instance) {
        if (__instance.loadAfter.Any(s => s.ToLowerInvariant() == PurePatcherMod.HarmonyModId) &&
            !__instance.loadAfter.Any(s => s.ToLowerInvariant() == PurePatcherMod.PurePatcherModId))
            __instance.loadAfter.Add(PurePatcherMod.PurePatcherModId);
    }

    private static bool IsSatisfiedPostfix(bool result, ModDependency __instance) {
        return result ||
               __instance.packageId.ToLowerInvariant() == PurePatcherMod.HarmonyModId &&
               ModLister.GetActiveModWithIdentifier(PurePatcherMod.PurePatcherModId, ignorePostfix: true) != null;
    }
}
