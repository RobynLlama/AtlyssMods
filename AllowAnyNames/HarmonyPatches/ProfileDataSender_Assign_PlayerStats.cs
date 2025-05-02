using HarmonyLib;
using System.Reflection.Emit;

namespace Marioalexsan.AllowAnyNames.HarmonyPatches;

[HarmonyPatch(typeof(ProfileDataSender), nameof(ProfileDataSender.Assign_PlayerStats))]
static class ProfileDataSender_Assign_PlayerStats
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
            new CodeMatch((ins) => 
            ins.opcode == OpCodes.Ldstr && 
            ins.operand != null && 
            ins.operand.GetType() == typeof(string) && 
            string.Equals((string)ins.operand, "Null", StringComparison.InvariantCultureIgnoreCase))
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