using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.NoPlayerCap;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class NoPlayerCapMod : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        _harmony.PatchAll();
    }
}