

using HarmonyLib;
using Marioalexsan.SettingsPlus;

[HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.Open_SettingsMenu))]
static class SettingsManager_Open_SettingsMenu
{
    static void Postfix(SettingsManager __instance)
    {
        SettingsPlusMod.Instance.InitMenu(__instance);
    }
}

[HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.Load_SettingsData))]
static class SettingsManager_Load_SettingsData
{
    static void Postfix(SettingsManager __instance)
    {
        SettingsPlusMod.Instance.LoadData(__instance);
    }
}

[HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.Save_SettingsData))]
static class SettingsManager_Save_SettingsData
{
    static void Postfix(SettingsManager __instance)
    {
        SettingsPlusMod.Instance.SaveData(__instance);
    }
}