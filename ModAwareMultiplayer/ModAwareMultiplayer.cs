using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.ModAwareMultiplayer;

[BepInPlugin("Marioalexsan.ModAwareMultiplayer", "ModAwareMultiplayer", "1.0.0")]
public class ModAwareMultiplayer : BaseUnityPlugin
{
    public static string ModdedNetworkApplicationVersion { get; private set; }

    private void Awake()
    {
        var harmony = new Harmony("Marioalexsan.ModAwareMultiplayer");
        harmony.PatchAll();

        ModdedNetworkApplicationVersion = Application.version + " (modded)";
    }
}