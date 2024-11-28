using HarmonyLib;

namespace Marioalexsan.AtlyssDiscordRichPresence.HarmonyPatches;

[HarmonyPatch(typeof(PatternInstanceManager), nameof(PatternInstanceManager.Update))]
static class PatternInstanceManager_Update
{
    static void Postfix(PatternInstanceManager __instance)
    {
        AtlyssDiscordRichPresenceMod.Instance.PatternInstanceManager_Update(__instance);
    }
}
