using BepInEx.Bootstrap;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine;

namespace Marioalexsan.AutoSaver.HarmonyReversePatches;

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Save_ItemStorageData))]
static class ItemBankAutoSaver
{
    public static void TrySaveCurrentProfileToLocation(string location)
    {
        if (SaveDone)
        {
            AutoSaver.Plugin.Logger.LogInfo("Triggering item bank save process...");
        }

        SaveLocationOverride = location;
        BanksDone = 0;
        SaveDone = false;
        SaveProfileData(ProfileDataManager._current);
    }

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
    private static string? SaveLocationOverride; // Directory
    private static string? TempContents;

    private static int BanksDone = 0;
    private static int BanksMax = 0;
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
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(TempContents))),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(SaveLocationOverride))),
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => MarkSaveDone(null!))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(TempContents)))
                    );

                matcher.Advance(1);
            }

            const int expectedLocations = 3;

            if (patchLocations != expectedLocations)
            {
                AutoSaver.Plugin.Logger.LogWarning($"WARNING: Expected {expectedLocations} patch locations, got {patchLocations}.");
                AutoSaver.Plugin.Logger.LogWarning($"Either the vanilla code changed, or mods added extra stuff. This may or may not cause issues.");
            }

            BanksMax = expectedLocations;

            return matcher.InstructionEnumeration();
        }

        _ = Transpiler(null!);
        throw new NotImplementedException("Stub method");
    }

    private static string MarkSaveDone(string location)
    {
        BanksDone++;

        if (BanksDone >= BanksMax)
        {
            SaveDone = true;
        }

        return Path.Combine(location, $"itembank_{BanksDone - 1}");
    }

    public static void SaveModBankTabsToLocation(string location)
    {
        try
        {
            AutoSaver.Plugin.Logger.LogInfo("Attempting to save MoreBankTabs data...");

            var moreBankTabsMod = Chainloader.PluginInfos[AutoSaver.MoreBankTabsIndentifier].Instance;

            var _itemStorageProfile_03 = AccessTools.Field(moreBankTabsMod.GetType(), "_itemStorageProfile_03").GetValue(moreBankTabsMod);
            var _itemStorageProfile_04 = AccessTools.Field(moreBankTabsMod.GetType(), "_itemStorageProfile_04").GetValue(moreBankTabsMod);
            var _itemStorageProfile_05 = AccessTools.Field(moreBankTabsMod.GetType(), "_itemStorageProfile_05").GetValue(moreBankTabsMod);
            var _itemDatas_03 = AccessTools.Field(moreBankTabsMod.GetType(), "_itemDatas_03").GetValue(moreBankTabsMod);
            var _itemDatas_04 = AccessTools.Field(moreBankTabsMod.GetType(), "_itemDatas_04").GetValue(moreBankTabsMod);
            var _itemDatas_05 = AccessTools.Field(moreBankTabsMod.GetType(), "_itemDatas_05").GetValue(moreBankTabsMod);

            var itemDatas03Array = AccessTools.Method(_itemDatas_03.GetType(), "ToArray").Invoke(_itemDatas_03, []);
            var itemDatas04Array = AccessTools.Method(_itemDatas_04.GetType(), "ToArray").Invoke(_itemDatas_04, []);
            var itemDatas05Array = AccessTools.Method(_itemDatas_05.GetType(), "ToArray").Invoke(_itemDatas_05, []);

            AccessTools.Field(_itemStorageProfile_03.GetType(), "_heldItemStorage").SetValue(_itemStorageProfile_03, itemDatas03Array);
            AccessTools.Field(_itemStorageProfile_04.GetType(), "_heldItemStorage").SetValue(_itemStorageProfile_04, itemDatas04Array);
            AccessTools.Field(_itemStorageProfile_05.GetType(), "_heldItemStorage").SetValue(_itemStorageProfile_05, itemDatas05Array);

            string contents03 = JsonUtility.ToJson(_itemStorageProfile_03, true);
            string contents04 = JsonUtility.ToJson(_itemStorageProfile_04, true);
            string contents05 = JsonUtility.ToJson(_itemStorageProfile_05, true);

            File.WriteAllText(Path.Combine(location, "MoreBankTabs_itemBank_03"), contents03);
            File.WriteAllText(Path.Combine(location, "MoreBankTabs_itemBank_04"), contents04);
            File.WriteAllText(Path.Combine(location, "MoreBankTabs_itemBank_05"), contents05);

            AutoSaver.Plugin.Logger.LogInfo("MoreBankTabs slots saved.");
        }
        catch (Exception e)
        {
            AutoSaver.Plugin.Logger.LogError("Failed to save MoreBankTabs info.");
            AutoSaver.Plugin.Logger.LogError($"Exception info: {e}");
        }
    }
}
