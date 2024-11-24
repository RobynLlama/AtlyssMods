using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TemplateMod;

[BepInPlugin("Marioalexsan.NoPlayerCap", "Raises server max player cap to 64", "1.0.0")]
public class TemplateMod : BaseUnityPlugin
{
    private void Awake()
    {
        On.ProfileDataManager.Load_HostSettingsData += ProfileDataManager_Load_HostSettingsData;
        On.LobbyListManager.Awake += LobbyListManager_Awake;
    }

    private void LobbyListManager_Awake(On.LobbyListManager.orig_Awake orig, LobbyListManager self)
    {
        orig(self);
        self._lobbyMaxConnectionSlider.maxValue = 64;
    }

    private void ProfileDataManager_Load_HostSettingsData(On.ProfileDataManager.orig_Load_HostSettingsData orig, ProfileDataManager self)
    {
        orig(self);

        if (File.Exists(self._dataPath + "/hostSettings.json"))
        {
            string json = File.ReadAllText(self._dataPath + "/hostSettings.json");
            self._hostSettingsProfile = JsonUtility.FromJson<ServerHostSettings_Profile>(json);

            // Let's keep the lower bound
            if (self._hostSettingsProfile._maxAllowedConnections < 2)
            {
                self._hostSettingsProfile._maxAllowedConnections = 2;
            }
            if (self._hostSettingsProfile._maxAllowedConnections > 64)
            {
                self._hostSettingsProfile._maxAllowedConnections = 64;
            }

            MainMenuManager._current.Load_HostSettings();
            AtlyssNetworkManager._current.maxConnections = self._hostSettingsProfile._maxAllowedConnections;
        }
    }
}