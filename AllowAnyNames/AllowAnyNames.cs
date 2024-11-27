using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.AllowAnyName;

[HarmonyPatch(typeof(ProfileDataSender), nameof(ProfileDataSender.Assign_PlayerStats))]
static class MenuPatch
{
    static void Throw(string reason)
    {
        throw new Exception($"Failed to transpile {nameof(ProfileDataSender.Assign_PlayerStats)}, please notify the mod developer about it! Reason: {reason}");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false, [
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.IsNullOrWhiteSpace)))
            ]);

        if (matcher.IsInvalid)
            Throw("Failed to find start.");

        var start = matcher.Advance(-1).Pos;

        if (!(matcher.Instruction.IsLdarg() || matcher.Instruction.IsLdloc()))
            Throw("Failed to find load arg/loc for start.");

        matcher.MatchForward(true, [
            new CodeMatch(OpCodes.Ldstr, "null")
            ]);

        if (matcher.IsInvalid)
            Throw("Failed to find end.");

        var end = matcher.Advance(1).Pos;

        if (!(matcher.Instruction.IsStarg() || matcher.Instruction.IsStloc()))
            Throw("Failed to find store arg/loc for end.");

        matcher.RemoveInstructionsInRange(start, end);

        return matcher.InstructionEnumeration();
    }
}

[BepInPlugin("Marioalexsan.AllowAnyNames", "Removes player name validation for hosts.", "1.0.0")]
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