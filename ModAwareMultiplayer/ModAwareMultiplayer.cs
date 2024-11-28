using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.ModAwareMultiplayer;

[BepInPlugin("Marioalexsan.ModAwareMultiplayer", "Template mod for Atlyss using BepInEx", "1.0.0")]
public class ModAwareMultiplayer : BaseUnityPlugin
{
    private void Awake()
    {
        var harmony = new Harmony("Marioalexsan.ModAwareMultiplayer");
        harmony.PatchAll();

        UnityEngine.Debug.Log("Hello from Marioalexsan.ModAwareMultiplayer!");
    }
}