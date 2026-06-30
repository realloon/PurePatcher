using JetBrains.Annotations;
using System.Threading;
using DataAssembly;
using UnityEngine;
using Verse;

namespace PurePatcher;

[UsedImplicitly]
internal class PurePatcherMod : Mod {
    private const string CmdArgVerbose = "verbose";

    public PurePatcherMod(ModContentPack content) : base(content) {
        InitLg();

        Patches.HarmonyPatches.AddVerboseProfiling();

        if (DataStore.StartedOnce) {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (_, args) => {
                Lg.Verbose($"ReflectionOnlyAssemblyResolve: {args.RequestingAssembly} requested {args.Name}");
                return null;
            };

            Lg.Info($"Restarted with the patched assembly, going silent.");
            return;
        }

        DataStore.StartedOnce = true;
        Lg.Info($"Starting... (vanilla load took {Time.realtimeSinceStartup}s)");

        Patches.HarmonyPatches.SilenceLogging();
        Loader.Reload();

        // Thread abortion counts as a crash
        Prefs.data.resetModsConfigOnCrash = false;

        Thread.CurrentThread.Abort();
    }

    private static void InitLg() {
        Lg.InfoFunc = msg => Log.Message($"PurePatcher: {msg}");
        Lg.ErrorFunc = msg => Log.Error($"PurePatcher Error: {msg}");

        if (GenCommandLine.CommandLineArgPassed(CmdArgVerbose))
            Lg.VerboseFunc = msg => Log.Message($"PurePatcher Verbose: {msg}");
    }

    public override string SettingsCategory() {
        return "PurePatcher";
    }
}