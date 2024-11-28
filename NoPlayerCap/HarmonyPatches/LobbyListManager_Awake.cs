using HarmonyLib;

namespace Marioalexsan.NoPlayerCap.HarmonyPatches;

[HarmonyPatch(typeof(LobbyListManager), nameof(LobbyListManager.Awake))]
static class LobbyListManager_Awake
{
    static void Postfix(LobbyListManager __instance)
    {
        __instance._lobbyMaxConnectionSlider.maxValue = 64;
    }
}
