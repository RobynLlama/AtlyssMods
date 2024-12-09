using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.WalkMode;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class WalkModeMod : BaseUnityPlugin
{
    public static WalkModeMod Instance { get; private set; }
    internal new ManualLogSource Logger { get; private set; }

    private Harmony _harmony;

    public float WalkMoveSpeedModifier { get; private set; } = 0.5f;
    public float AnimationModifier { get; private set; } = 1f;
    public KeyCode Keybinding { get; private set; } = KeyCode.LeftControl;

    public bool IsWalking { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        Logger.LogInfo("Patching methods...");
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        Logger.LogInfo("Configuring...");
        Configure();

        Logger.LogInfo("Initialized successfully!");
    }

    private void Configure()
    {
        WalkMoveSpeedModifier = Config.Bind("General", "WalkSpeedModifier", 0.5f, "Movement speed when walking, expresses as a ratio (min 0.1, max 0.9).").Value;
        AnimationModifier = Config.Bind("General", "AnimationModifier", 1f, "How much animation gets impacted by walking speed (min 0.0 = zero impact, max 1.0 = slows down proportional to walk speed).").Value;
        Keybinding = Config.Bind("General", "WalkKeybind", KeyCode.LeftControl, "Keybinding to use for walking.").Value;

        WalkMoveSpeedModifier = Math.Max(Math.Min(WalkMoveSpeedModifier, 0.9f), 0.1f);
        AnimationModifier = Math.Max(Math.Min(AnimationModifier, 1f), 0f);
    }

    private void Update()
    {
        IsWalking = Input.GetKey(Keybinding);
    }
}

[HarmonyPatch]
static class WalkAnimPatch
{
    static MethodInfo TargetMethod()
    {
        return AccessTools.GetDeclaredMethods(typeof(PlayerVisual)).First(x => x.Name.Contains("Iterate_AnimationCondition"));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldstr, "_movAnimSpd")
            );

        if (matcher.IsInvalid)
            throw new InvalidOperationException($"Failed to find patch point in {nameof(PlayerVisual)}.Iterate_AnimationCondition (1/2). Notify the mod developer about this!");

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Animator), nameof(Animator.SetFloat), [typeof(string), typeof(float)]))
            );

        if (matcher.IsInvalid)
            throw new InvalidOperationException($"Failed to find patch point in {nameof(PlayerVisual)}.Iterate_AnimationCondition (2/2). Notify the mod developer about this!");

        matcher.Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WalkAnimPatch), nameof(EditAnimationSpeed)))
            );

        return matcher.InstructionEnumeration();
    }

    private static float EditAnimationSpeed(float animSpeed)
    {
        if (WalkModeMod.Instance.IsWalking)
        {
            var animationModifier = WalkModeMod.Instance.AnimationModifier;
            return animSpeed * ((1 - animationModifier) + animationModifier * WalkModeMod.Instance.WalkMoveSpeedModifier);
        }

        return animSpeed;
    }
}

[HarmonyPatch]
static class WalkPatch
{
    static MethodInfo TargetMethod()
    {
        return AccessTools.GetDeclaredMethods(typeof(PlayerMove)).First(x => x.Name.Contains("Apply_MovementParams"));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerMove), nameof(PlayerMove._worldSpaceInput))),
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CharacterController), nameof(CharacterController.Move)))
            );

        if (matcher.IsInvalid)
            throw new InvalidOperationException($"Failed to find patch point in {nameof(PlayerMove)}.Apply_MovementParams. Notify the mod developer about this!");

        matcher.Advance(1);
        matcher.Insert(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WalkPatch), nameof(EditMoveSpeed)))
            );

        return matcher.InstructionEnumeration();
    }

    private static Vector3 EditMoveSpeed(Vector3 input)
    {
        if (WalkModeMod.Instance.IsWalking)
            return input * WalkModeMod.Instance.WalkMoveSpeedModifier;

        return input;
    }
}