using HarmonyLib;
using System.IO;
using UnityEngine;

namespace Marioalexsan.NoPlayerCap.HarmonyPatches;

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Load_HostSettingsData))]
static class ProfileDataManager_Load_HostSettingsData
{
    static void Postfix(ProfileDataManager __instance)
    {
        if (File.Exists(__instance._dataPath + "/hostSettings.json"))
        {
            string json = File.ReadAllText(__instance._dataPath + "/hostSettings.json");
            __instance._hostSettingsProfile = JsonUtility.FromJson<ServerHostSettings_Profile>(json);

            // Let's keep the lower bound
            if (__instance._hostSettingsProfile._maxAllowedConnections < 2)
            {
                __instance._hostSettingsProfile._maxAllowedConnections = 2;
            }
            if (__instance._hostSettingsProfile._maxAllowedConnections > 64)
            {
                __instance._hostSettingsProfile._maxAllowedConnections = 64;
            }

            MainMenuManager._current.Load_HostSettings();
            AtlyssNetworkManager._current.maxConnections = __instance._hostSettingsProfile._maxAllowedConnections;
        }
    }
}
