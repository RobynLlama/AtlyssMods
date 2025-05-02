using HarmonyLib;

namespace Marioalexsan.AtlyssDiscordRichPresence.HarmonyPatches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Set_MenuCondition))]
static class MainMenuManager_Set_MenuCondition
{
    static void Postfix(MainMenuManager __instance)
    {
        AtlyssDiscordRichPresence.Plugin.MainMenuManager_Set_MenuCondition(__instance);
    }
}
