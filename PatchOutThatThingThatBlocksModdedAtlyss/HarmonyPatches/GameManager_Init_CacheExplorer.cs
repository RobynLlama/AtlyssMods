using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marioalexsan.ModAwareMultiplayer.HarmonyPatches;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.Init_CacheExplorer))]
internal class GameManager_Init_CacheExplorer
{
    static bool Prefix()
    {
        return false;
    }
}
