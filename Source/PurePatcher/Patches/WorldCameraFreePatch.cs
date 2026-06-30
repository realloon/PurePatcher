using Mono.Cecil;
using MonoMod.Utils;
using UnityEngine;
using RimWorld.Planet;
using PurePatcher.Annotations;

namespace PurePatcher.Patches;

public static class WorldCameraFreePatch {
    [FreePatch]
    public static void ReplaceAddComponent(ModuleDefinition module) {
        var type = module.GetType($"RimWorld.Planet.{nameof(WorldCameraManager)}");
        var method = type.FindMethod(nameof(WorldCameraManager.CreateWorldCamera)) ??
                     throw new MissingMethodException(type.FullName, nameof(WorldCameraManager.CreateWorldCamera));

        // For some reason, AddComponent treats WorldCameraDriver by name causing it to be loaded from old Assembly-CSharp
        foreach (var inst in method.Body.Instructions) {
            if (inst.Operand is MethodReference { Name: nameof(GameObject.AddComponent) }) {
                inst.Operand = module.ImportReference(typeof(WorldCameraFreePatch).GetMethod(nameof(AddComponent)));
            }
        }
    }

    public static Component AddComponent(GameObject gameObject) => gameObject.AddComponent<WorldCameraDriver2>();
}