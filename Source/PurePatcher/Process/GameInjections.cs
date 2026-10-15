using RimWorld.Planet;
using Verse;

namespace PurePatcher.Process;

internal static class GameInjections {
    // Private in 1.7, so nameof is unavailable.
    private const string ThingCompsField = "comps";
    private const string FillComponentsMethod = "FillComponents";

    internal static void RegisterInjections(FieldAdder fieldAdder) {
        Logger.Verbose("Registering injections");

        fieldAdder.RegisterInjection(
            typeof(ThingWithComps),
            typeof(ThingComp),
            nameof(ThingWithComps.InitializeComps),
            ThingCompsField
        );

        fieldAdder.RegisterInjection(
            typeof(Map),
            typeof(MapComponent),
            FillComponentsMethod,
            nameof(Map.components)
        );

        fieldAdder.RegisterInjection(
            typeof(World),
            typeof(WorldComponent),
            FillComponentsMethod,
            nameof(World.components)
        );

        fieldAdder.RegisterInjection(
            typeof(Game),
            typeof(GameComponent),
            FillComponentsMethod,
            nameof(Game.components)
        );
    }
}