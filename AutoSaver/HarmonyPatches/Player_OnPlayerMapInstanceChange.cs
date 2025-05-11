using HarmonyLib;

namespace Marioalexsan.AutoSaver.HarmonyPatches;

[HarmonyPatch(typeof(Player), nameof(Player.OnPlayerMapInstanceChange))]
static class Player_OnPlayerMapInstanceChange
{
    static void Postfix(Player __instance, MapInstance _new)
    {
        if (__instance == Player._mainPlayer)
        {
            AutoSaver.Plugin.GameEntered();
        }
    }
}
