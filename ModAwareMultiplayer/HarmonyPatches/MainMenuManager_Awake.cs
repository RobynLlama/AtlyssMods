using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marioalexsan.ModAwareMultiplayer.HarmonyPatches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
static class MainMenuManager_Awake
{
    static void Prefix()
    {
        ModAwareMultiplayer.CheckModVanillaCompatibility();
    }
}
