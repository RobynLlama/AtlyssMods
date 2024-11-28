using HarmonyLib;

namespace Marioalexsan.AtlyssDiscordRichPresence.HarmonyPatches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Set_MenuCondition))]
static class MainMenuManager_Set_MenuCondition
{
    static void Postfix(MainMenuManager __instance)
    {
        AtlyssDiscordRichPresenceMod.Instance.MainMenuManager_Set_MenuCondition(__instance);
    }
}
