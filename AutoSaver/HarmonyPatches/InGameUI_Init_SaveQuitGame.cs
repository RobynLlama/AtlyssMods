using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Marioalexsan.AutoSaver.HarmonyPatches;

[HarmonyPatch]
static class InGameUI_Init_SaveQuitGame
{
    static MethodInfo TargetMethod()
    {
        return Application.version switch
        {
            "Beta 1.6.2b" => AccessTools.Method("InGameUI:Init_SaveQuitGame"),
            "Beta 2.0.5d" => AccessTools.Method("OptionsMenuCell:Init_SaveQuitGame"),
            _ => AccessTools.Method("OptionsMenuCell:Init_SaveQuitGame")
        };
    }

    static void Prefix()
    {
        AutoSaver.Plugin.GameExited();
    }
}
