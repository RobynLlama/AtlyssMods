using HarmonyLib;

namespace Marioalexsan.AtlyssDiscordRichPresence.HarmonyPatches;

[HarmonyPatch(typeof(Player), nameof(Player.OnPlayerMapInstanceChange))]
static class Player_OnPlayerMapInstanceChange
{
    static void Postfix(Player __instance, MapInstance _new)
    {
        AtlyssDiscordRichPresence.Plugin.Player_OnPlayerMapInstanceChange(__instance, _new);
    }
}