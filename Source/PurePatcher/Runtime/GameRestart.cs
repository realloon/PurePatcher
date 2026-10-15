using UnityEngine;
using Waylith;

namespace PurePatcher.Runtime;

internal static class GameRestart {
    internal static void Restart() {
        RestartManager.RequestRestart();
        Application.Quit();
    }
}