using System.Collections;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace PurePatcher.Patches;

internal static partial class HarmonyPatches {
    internal static void PatchRootMethods() {
        Logger.Verbose("Patching Root methods");

        Harmony.Patch(
            Loader.OrigAsm.GetType("Verse.Root_Play").GetMethod("Start"),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(EmptyTranspiler))
        );

        Harmony.Patch(
            Loader.OrigAsm.GetType("Verse.Root_Entry").GetMethod("Start"),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(EmptyTranspiler))
        );

        Harmony.Patch(
            Loader.OrigAsm.GetType("Verse.Root_Play").GetMethod("Update"),
            new HarmonyMethod(typeof(HarmonyPatches), nameof(RootUpdatePrefix))
        );

        Harmony.Patch(
            Loader.OrigAsm.GetType("Verse.Root_Entry").GetMethod("Update"),
            new HarmonyMethod(typeof(HarmonyPatches), nameof(RootUpdatePrefix))
        );
    }

    private static bool _rootUpdateRunOnce;

    // ReSharper disable once InconsistentNaming
    private static bool RootUpdatePrefix(Root __instance) {
        if (!Loader.RestartGame) return false;

        if (!_rootUpdateRunOnce) {
            // Done to prevent a brief flash of black
            __instance.StartCoroutine(RecreateAtEndOfFrame());
            _rootUpdateRunOnce = true;
        } else {
            RecreateComponents();
        }

        return false;
    }

    private static IEnumerator RecreateAtEndOfFrame() {
        yield return new WaitForEndOfFrame();
        RecreateComponents();
    }

    private static void RecreateComponents() {
        Logger.Verbose("Recreating comps");

        // It's important the components are iterated this way to make sure
        // they are recreated in the correct order.
        foreach (var comp in UnityEngine.Object.FindObjectsOfType<Component>()) {
            var compType = comp.GetType();
            if (compType.Assembly == Loader.NewAsm) continue;

            var translation = Loader.NewAsm.GetType(compType.FullName!);
            if (translation == null) continue;

            try {
                var newComp = comp.gameObject.AddComponent(translation);
                UnityEngine.Object.Destroy(comp);

                Logger.Verbose(
                    $"Recreated {comp} {newComp.GetType().Assembly == Loader.NewAsm} with new type {translation.FullName}");
            } catch (Exception e) {
                Logger.Error($"Exception recreating Unity component {comp}: {e}");
            }
        }
    }

    private static IEnumerable<CodeInstruction> EmptyTranspiler(IEnumerable<CodeInstruction> _) {
        yield return new CodeInstruction(OpCodes.Ret);
    }
}