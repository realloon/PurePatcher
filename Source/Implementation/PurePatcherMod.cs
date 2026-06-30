using System.Threading;
using DataAssembly;
using UnityEngine;
using Verse;

namespace PurePatcher;

internal class PurePatcherMod : Mod {
    private const string CmdArgVerbose = "verbose";

    internal const string PurePatcherModId = "vortex.purepatcher";
    internal const string HarmonyModId = "brrainz.harmony";

    public PurePatcherMod(ModContentPack content) : base(content) {
        InitLg();

        HarmonyPatches.PatchModLoading();
        HarmonyPatches.AddVerboseProfiling();

        if (DataStore.startedOnce) {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, args) => {
                Lg.Verbose($"ReflectionOnlyAssemblyResolve: {args.RequestingAssembly} requested {args.Name}");
                return null;
            };

            Lg.Info($"Restarted with the patched assembly, going silent.");
            return;
        }

        DataStore.startedOnce = true;
        Lg.Info($"Starting... (vanilla load took {Time.realtimeSinceStartup}s)");

        HarmonyPatches.SilenceLogging();
        Loader.Reload();

        // Thread abortion counts as a crash
        Prefs.data.resetModsConfigOnCrash = false;

        Thread.CurrentThread.Abort();
    }

    private static void InitLg() {
        Lg._infoFunc = msg => Log.Message($"PurePatcher: {msg}");
        Lg._errorFunc = msg => Log.Error($"PurePatcher Error: {msg}");

        if (GenCommandLine.CommandLineArgPassed(CmdArgVerbose))
            Lg._verboseFunc = msg => Log.Message($"PurePatcher Verbose: {msg}");
    }

    public override string SettingsCategory() {
        return "PurePatcher";
    }
}