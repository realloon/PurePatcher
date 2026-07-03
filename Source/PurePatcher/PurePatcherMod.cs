using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace PurePatcher;

[UsedImplicitly]
internal class PurePatcherMod : Mod {
    private const string CmdArgVerbose = "verbose";

    public PurePatcherMod(ModContentPack content) : base(content) {
        InitLogger();

        if (BootstrapState.StartedOnce) {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (_, args) => {
                Logger.Verbose($"ReflectionOnlyAssemblyResolve: {args.RequestingAssembly} requested {args.Name}");
                return null;
            };

            Logger.Info($"Restarted with the patched assembly, going silent.");
            return;
        }

        BootstrapState.MarkStartedOnce();
        Logger.Info($"Starting... (vanilla load took {Time.realtimeSinceStartup}s)");

        Patches.HarmonyPatches.SilenceLogging();
        Loader.Reload();

        // Thread abortion counts as a crash
        Prefs.data.resetModsConfigOnCrash = false;

        Thread.CurrentThread.Abort();
    }

    private static void InitLogger() {
        Logger.InfoFunc = msg => Log.Message("[PurePatcher]: " + msg);
        Logger.ErrorFunc = msg => Log.Error($"[PurePatcher]: " + msg);

        if (GenCommandLine.CommandLineArgPassed(CmdArgVerbose)) {
            Logger.VerboseFunc = msg => Log.Message($"PurePatcher Verbose: {msg}");
        }
    }

    public override string SettingsCategory() => "PurePatcher";
}