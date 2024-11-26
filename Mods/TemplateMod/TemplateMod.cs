using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TemplateMod;

// Comment this patch if you want to use HookGen instead of Harmony
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Set_MenuCondition))]
public static class MenuPatch
{
    [HarmonyPostfix]
    private static void SetMenuConditionPatch(MainMenuManager __instance)
    {
        __instance._versionDisplayText.text = Application.version + " with Mods :D";
    }
}

[BepInPlugin("Marioalexsan.TemplateMod", "Template mod for Atlyss using BepInEx", "1.0.0")]
public class TemplateMod : BaseUnityPlugin
{
    // The template mod patches the main menu version string to include extra text.

    private void Awake()
    {
        var harmony = new Harmony("TemplateMod");
        harmony.PatchAll();

        UnityEngine.Debug.Log("Hello from TemplateMod!");
        UnityEngine.Debug.Log($"Application version is ${Application.version}");

        SetupMonomodHooks();
    }

    // Uncomment the following stuff if you want to use AutoHookGenPatcher / HookGen
    // Also comment out the Harmony patch if you do so
    // (you'll need MMHOOK_Assembly-CSharp.dll)

    private void SetupMonomodHooks()
    {
        //On.MainMenuManager.Set_MenuCondition += MainMenuManager_Set_MenuCondition;
    }

    //public void MainMenuManager_Set_MenuCondition(On.MainMenuManager.orig_Set_MenuCondition orig, MainMenuManager self, int _index)
    //{
    //    orig(self, _index);
    //    self._versionDisplayText.text = Application.version + " with Mods :D";
    //}
}