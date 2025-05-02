using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace Marioalexsan.AllowAnyNames.HarmonyPatches;

[HarmonyPatch]
static class CharacterSelectManager_Handle_ButtonControl
{
    public const string ReplacementDeleteString = "delete this";

    static void Throw(string reason)
    {
        throw new Exception($"Failed to transpile {"Handle_ButtonControl"}, please notify the mod developer about it! Reason: {reason}");
    }

    static MethodInfo TargetMethod()
    {
        return AccessTools.GetDeclaredMethods(typeof(CharacterSelectManager)).First(x => x.Name.Contains("Handle_ButtonControl"));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CharacterFile), nameof(CharacterFile._nickName)))
            );

        if (matcher.IsInvalid)
            Throw("Failed to find nickname field load.");

        matcher.Advance(1);
        matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterSelectManager_Handle_ButtonControl), nameof(ReplaceExpectedNickname))));

        return matcher.InstructionEnumeration();
    }

    static string ReplaceExpectedNickname(string input)
    {
        _ = input;
        return ReplacementDeleteString;
    }
}
