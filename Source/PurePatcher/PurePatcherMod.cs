using JetBrains.Annotations;
using Verse;

namespace PurePatcher;

[UsedImplicitly]
internal class PurePatcherMod : Mod {
    private const string CmdArgVerbose = "verbose";

    public PurePatcherMod(ModContentPack content) : base(content) {
        InitLogger();
        Loader.Reload(content.PackageId);
    }

    private static void InitLogger() {
        Logger.InfoFunc = msg => Log.Message("[PurePatcher]: " + msg);
        Logger.ErrorFunc = msg => Log.Error($"[PurePatcher]: " + msg);

        if (GenCommandLine.CommandLineArgPassed(CmdArgVerbose)) {
            Logger.VerboseFunc = msg => Log.Message($"PurePatcher Verbose: {msg}");
        }
    }
}