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

[BepInPlugin("Marioalexsan.ModAwareMultiplayer", "ModAwareMultiplayer", "1.1.0")]
public class ModAwareMultiplayer : BaseUnityPlugin
{
    private static ModAwareMultiplayer Instance;

    internal static void CheckModVanillaCompatibility()
    {
        Instance.Logger.LogInfo("Checking vanilla compatiblity.");
        int incompatibleMods = 0;

        foreach (var mod in Chainloader.PluginInfos)
        {
            if (mod.Key == "Marioalexsan.ModAwareMultiplayer")
            {
                continue;
            }

            if (mod.Value.Instance.GetType().GetCustomAttributes(true).Any(x => x.GetType().Name.Contains("ModAwareMultiplayerVanillaCompatible")))
            {
                Instance.Logger.LogInfo($"Mod {mod.Key} has custom attribute, will deem as compatible.");
            }
            else
            {
                Instance.Logger.LogInfo($"Mod {mod.Key} does not declare compatibility.");
                incompatibleMods++;
            }
        }

        bool compatible = incompatibleMods == 0;

        Instance.Logger.LogInfo($"Deemed mod list as being vanilla {(compatible ? "compatible" : "incompatible")}.");
        SetVanillaCompatibility(compatible);
    }

    public static string VanillaApplicationVersion { get; private set; }

    public static string ModdedNetworkApplicationVersion { get; private set; }

    public static string ModdedDisplayApplicationVersion { get; private set; }

    private static void SetVanillaCompatibility(bool compatible)
    {
        if (compatible)
        {
            ModdedDisplayApplicationVersion = Application.version + " (vanilla)";
            ModdedNetworkApplicationVersion = Application.version;
        }
        else
        {
            ModdedDisplayApplicationVersion = Application.version + " (modded)";
            ModdedNetworkApplicationVersion = Application.version + " (modded)";
        }
    }

    private void Awake()
    {
        Instance = this;

        var harmony = new Harmony("Marioalexsan.ModAwareMultiplayer");
        harmony.PatchAll();

        VanillaApplicationVersion = Application.version;
        SetVanillaCompatibility(false);
    }
}