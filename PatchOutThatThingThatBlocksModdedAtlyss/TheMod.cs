using System.Linq;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.ModAwareMultiplayer;

[BepInPlugin("Marioalexsan.PatchOutThatThingThatBlocksModdedAtlyss", "PatchOutThatThingThatBlocksModdedAtlyss", "1.0.0")]
public class ModAwareMultiplayer : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("Marioalexsan.PatchOutThatThingThatBlocksModdedAtlyss");
        _harmony.PatchAll();
        Logger.LogInfo("Patched out the code that autokicks modded clients and servers.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
        Logger.LogInfo("IMPORTANT: Please don't use this mod to mess with vanilla players, thanks.");
    }
}