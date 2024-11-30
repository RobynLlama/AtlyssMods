using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.TemplateMod;

[BepInPlugin("Marioalexsan.TemplateMod", "TemplateMod", "1.0.0")]
public class NoPlayerCapMod : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("Marioalexsan.NoPlayerCap");
        _harmony.PatchAll();
    }
}