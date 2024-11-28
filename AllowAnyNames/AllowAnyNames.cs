using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.AllowAnyNames;

[BepInPlugin("Marioalexsan.AllowAnyNames", "AllowAnyNames", "1.0.0")]
public class AllowAnyNamesMod : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("Marioalexsan.AllowAnyNames");
        _harmony.PatchAll();

        UnityEngine.Debug.Log("Hello from Marioalexsan.AllowAnyNames!");
    }
}