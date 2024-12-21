using HarmonyLib;

namespace Marioalexsan.AutoSaver.HarmonyPatches;

[HarmonyPatch(typeof(InGameUI), nameof(InGameUI.Init_SaveQuitGame))]
static class InGameUI_Init_SaveQuitGame
{
    static void Prefix()
    {
        AutoSaverMod.Instance.GameExited();
    }
}
