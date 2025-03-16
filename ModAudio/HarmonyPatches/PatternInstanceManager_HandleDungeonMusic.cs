using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Marioalexsan.ModAudio.HarmonyPatches;

// Why is this the only place that requires a specific patch for ATLYSS? Ugh

// Makes the post-boss defeat music switch apply one time only, instead of continuously
[HarmonyPatch]
static class PatternInstanceManager_HandleDungeonMusic
{
    static MethodInfo TargetMethod() => typeof(PatternInstanceManager).GetMethods(AccessTools.all).First(x => x.Name.Contains("Handle_DungeonMusic"));

    private static PatternInstanceManager Manager { get; set; }

    private static bool MusicSwitched { get; set; }

    static void Prefix(PatternInstanceManager __instance)
    {
        if (!Manager)
        {
            Logging.LogInfo("Patch: Boss music unswitched.");
            MusicSwitched = false;
        }

        Manager = __instance;

        if (!__instance._muAmbienceSrc || !__instance._muActionSrc)
        {
            Logging.LogInfo("Patch: Boss music unswitched.");
            MusicSwitched = false;
        }
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PatternInstanceManager), nameof(PatternInstanceManager._muBossSrc))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(AudioSource), nameof(AudioSource.clip))),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PatternInstanceManager), nameof(PatternInstanceManager._clearMusic))),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality")),
            new CodeMatch(OpCodes.Brfalse)
            );

        if (matcher.IsInvalid)
        {
            Logging.LogWarning("Failed to patch PatternInstanceManager::HandleDungeonMusic - couldn't find 'if (this._muBossSrc.clip != this._clearMusic)'!");
            Logging.LogWarning("This likely means that post-boss music replacements will fail to be applied correctly.");
            Logging.LogWarning("Please notify the mod creator about this!");
            return matcher.InstructionEnumeration();
        }

        var checkerPos = matcher.Pos;

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PatternInstanceManager), nameof(PatternInstanceManager._muBossSrc))),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PatternInstanceManager), nameof(PatternInstanceManager._clearMusic))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertySetter(typeof(AudioSource), nameof(AudioSource.clip)))
            );

        if (matcher.IsInvalid)
        {
            Logging.LogWarning("Failed to patch PatternInstanceManager::HandleDungeonMusic - couldn't find 'this._muBossSrc.clip = this._clearMusic'!");
            Logging.LogWarning("This likely means that post-boss music replacements will fail to be applied correctly.");
            Logging.LogWarning("Please notify the mod creator about this!");
            return matcher.InstructionEnumeration();
        }

        var setterPos = matcher.Pos;

        // Do some edits - mind the order of insertions (need to insert from the "end" of of the method towards the "start")

        matcher.Start();
        matcher.Advance(setterPos);

        matcher.Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatternInstanceManager_HandleDungeonMusic), nameof(SetMusicSwitched)))
            );

        matcher.Start();
        matcher.Advance(checkerPos + 5);

        matcher.Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PatternInstanceManager_HandleDungeonMusic), nameof(ManipulateClearMusic)))
            );

        return matcher.InstructionEnumeration();
    }

    private static AudioClip ManipulateClearMusic(AudioClip clip)
    {
        return MusicSwitched ? Manager._muBossSrc.clip : clip;
    }

    private static void SetMusicSwitched()
    {
        Logging.LogInfo("Patch: Boss music switched.");
        MusicSwitched = true;
    }
}
