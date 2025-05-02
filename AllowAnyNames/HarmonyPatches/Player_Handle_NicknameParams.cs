using HarmonyLib;
using System.Reflection.Emit;

namespace Marioalexsan.AllowAnyNames.HarmonyPatches;

[HarmonyPatch(typeof(Player), nameof(Player.Handle_ServerParameters))]
static class Player_Handle_ServerParameters
{
    static void Throw(string reason)
    {
        throw new Exception($"Failed to transpile {nameof(Player.Handle_ServerParameters)}, please notify the mod developer about it! Reason: {reason}");
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var method = AccessTools.GetDeclaredMethods(typeof(Player)).FirstOrDefault(x => x.Name.Contains("Handle_NicknameParams"));

        if (method == null)
            Throw("Failed to find method.");

        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false, [
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, method)
            ]);

        if (matcher.IsInvalid)
            Throw("Failed to find method.");

        var index = matcher.Pos;

        // This likely has a jump before it, let's move the labels
        matcher.InstructionAt(2).labels = new List<Label>(matcher.InstructionAt(0).labels);
        matcher.InstructionAt(0).labels.Clear();

        matcher.RemoveInstructionsInRange(index, index + 1);

        return matcher.InstructionEnumeration();
    }
}