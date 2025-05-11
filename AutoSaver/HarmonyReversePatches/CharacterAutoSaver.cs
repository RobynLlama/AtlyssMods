using HarmonyLib;
using System.Reflection.Emit;

namespace Marioalexsan.AutoSaver.HarmonyReversePatches;

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Save_ProfileData))]
static class CharacterAutoSaver
{
    public static void TrySaveCurrentProfileToLocation(string location)
    {
        if (SaveDone)
        {
            AutoSaver.Plugin.Logger.LogInfo("Triggering character save process...");
        }

        SaveLocationOverride = location;
        SaveDone = false;
        TargetPlayer = Player._mainPlayer;
        SaveProfileData(ProfileDataManager._current);
    }

    public static void TrySaveSpecificProfileToLocation(Player player, string location)
    {
        if (SaveDone)
        {
            AutoSaver.Plugin.Logger.LogInfo("Triggering character save process...");
        }

        SaveLocationOverride = location;
        SaveDone = false;
        TargetPlayer = player;
        SaveProfileData(ProfileDataManager._current);
    }

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
    private static string? SaveLocationOverride;
    private static string? TempContents;
    private static Player? TargetPlayer;
    internal static bool SaveDone { get; private set; } = true;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.Last)]
    private static void SaveProfileData(ProfileDataManager __instance)
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> data)
        {
            var matcher = new CodeMatcher(data);

            // Strategy: everywhere the save is about to be saved, drop the input path and use our own

            int patchLocations = 0;

            while (true)
            {
                matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => File.WriteAllText(null, null)))
                    );

                if (matcher.IsInvalid)
                    break;

                patchLocations++;

                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(CharacterAutoSaver), nameof(TempContents))),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CharacterAutoSaver), nameof(SaveLocationOverride))),
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => MarkSaveDone(null!))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CharacterAutoSaver), nameof(TempContents)))
                    );

                matcher.Advance(1);
            }

            const int expectedLocations = 2;

            if (patchLocations != expectedLocations)
            {
                AutoSaver.Plugin.Logger.LogWarning($"WARNING: CharacterAutoSaver expected {expectedLocations} patch locations, got {patchLocations}.");
                AutoSaver.Plugin.Logger.LogWarning($"Either the vanilla code changed, or mods added extra stuff. This may or may not cause issues.");
            }

            matcher.Start();
            patchLocations = 0;

            while (true)
            {
                matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Player), nameof(Player._mainPlayer)))
                    );

                if (matcher.IsInvalid)
                    break;

                var labels = matcher.Instruction.labels.ToList();
                matcher.RemoveInstruction();
                matcher.InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CharacterAutoSaver), nameof(TargetPlayer))).WithLabels(labels)
                    );

                patchLocations++;
            }

            AutoSaver.Plugin.Logger.LogInfo($"Patched {patchLocations} instances of Player._mainPlayer.");

            return matcher.InstructionEnumeration();
        }

        _ = Transpiler(null!);
        throw new NotImplementedException("Stub method");
    }

    private static string MarkSaveDone(string location)
    {
        SaveDone = true;
        return location;
    }
}
