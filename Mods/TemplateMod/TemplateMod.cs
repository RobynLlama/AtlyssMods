using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TemplateMod;

[BepInPlugin("Marioalexsan.TemplateMod", "Template mod for Atlyss using BepInEx", "1.0.0")]
public class TemplateMod : BaseUnityPlugin
{
    private void Awake()
    {
        UnityEngine.Debug.Log("Hello from TemplateMod!");
        UnityEngine.Debug.Log($"Application version is ${Application.version}");
        On.MainMenuManager.Set_MenuCondition += MainMenuManager_Set_MenuCondition;
    }

    public void MainMenuManager_Set_MenuCondition(On.MainMenuManager.orig_Set_MenuCondition orig, MainMenuManager self, int _index)
    {
        orig(self, _index);
        self._versionDisplayText.text = Application.version + " with Mods :D";
    }
}