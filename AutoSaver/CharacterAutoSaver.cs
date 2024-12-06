using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.TemplateMod;

[BepInPlugin("Marioalexsan.AutoSaver", "AutoSaver", "1.0.0")]
public class AutoSaverMod : BaseUnityPlugin
{
    public new ManualLogSource Logger { get; private set; }

    public static AutoSaverMod Instance { get; private set; }

    private Harmony _harmony;
    private TimeSpan _elapsedTime;
    private bool _characterActive;

    private bool AutosaveActive => _characterActive;

    private readonly char[] BannedChars = [.. Path.GetInvalidPathChars(), .. Path.GetInvalidFileNameChars()];

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        Logger.LogInfo("AutoSaver patching...");
        _harmony = new Harmony("Marioalexsan.AutoSaver");
        _harmony.PatchAll();

        Logger.LogInfo("AutoSaver configuring...");
        Configure();

        Logger.LogInfo("AutoSaver initialized!");
    }

    private TimeSpan _autosaveInterval;
    private int _saveCountToKeep;

    private void Configure()
    {
        int autosaveMinutes = Config.Bind("General", "BackupInterval", 4, "Interval between save backups, in minutes (min 1, max 60).").Value;

        autosaveMinutes = Math.Max(Math.Min(autosaveMinutes, 60), 1);
        _autosaveInterval = TimeSpan.FromMinutes(autosaveMinutes);

        _saveCountToKeep = Config.Bind("General", "SavesToKeep", 15, "Maximum number of saves to keep (min 5, max 50).").Value;
        _saveCountToKeep = Math.Max(Math.Min(_saveCountToKeep, 50), 5);
    }

    internal void GameEntered()
    {
        ProfileDataManager._current.Load_ItemStorageData(); // This isn't loaded on start for some reason
        RunAutosaves();
        Logger.LogInfo("Game entered. Activating character autosaves.");
        _elapsedTime = TimeSpan.Zero;
        _characterActive = true;
    }

    internal void GameExited()
    {
        Logger.LogInfo("Game exited. Stopping character autosaves.");
        RunAutosaves();
        _elapsedTime = TimeSpan.Zero;
        _characterActive = false;
    }

    private void Update()
    {
        if (AutosaveActive)
        {
            _elapsedTime += TimeSpan.FromSeconds(Time.deltaTime);

            if (_elapsedTime >= _autosaveInterval)
            {
                _elapsedTime = TimeSpan.Zero;
                RunAutosaves();
            }
            else
            {
                // Retry if player is buffering or w/e
                if (!CharacterAutoSaver.SaveDone)
                {
                    AutosaveCurrentCharacter();
                }
                if (!ItemBankAutoSaver.SaveDone)
                {
                    AutosaveCurrentItemBank();
                }
            }
        }
    }

    private void RunAutosaves()
    {
        AutosaveCurrentCharacter();
        AutosaveCurrentItemBank();
    }

    public string SanitizedCurrentTime
    {
        get
        {
            var time = DateTime.UtcNow.ToString("yyyy:MM:dd-HH:mm:ss", CultureInfo.InvariantCulture);

            for (int i = 0; i < BannedChars.Length; i++)
            {
                time = time.Replace($"{BannedChars[i]}", "_");
            }

            return time;
        }
    }

    public string SanitizedPlayerName
    {
        get
        {
            var playerName = Player._mainPlayer._nickname;

            for (int i = 0; i < BannedChars.Length; i++)
            {
                playerName = playerName.Replace($"{BannedChars[i]}", $"_{i}");
            }

            return playerName;
        }
    }

    private string ModDataFolderName = "Marioalexsan_AutoSaver";
    private string ModDataFolderPath => Path.Combine(ProfileDataManager._current._dataPath, ModDataFolderName);
    private string CharacterFolderPath => Path.Combine(ModDataFolderPath, "Characters");
    private string ItemBankFolderPath => Path.Combine(ModDataFolderPath, "ItemBank");

    private void RunItemBankGarbageCollector()
    {
        List<string> names = [];
        foreach (var directory in Directory.EnumerateDirectories(ItemBankFolderPath))
        {
            names.Add(Path.GetFileName(directory));
        }

        names.Remove("_latest");
        names.Sort();

        while (names.Count > _saveCountToKeep)
        {
            var saveToDelete = names[0];
            names.RemoveAt(0);

            var targetPath = Path.Combine(ItemBankFolderPath, saveToDelete);

            // Failsafe
            if (!targetPath.Contains(ModDataFolderName))
                throw new InvalidOperationException($"Got an invalid folder to delete {targetPath}, please notify the mod developer!");

            Directory.Delete(Path.Combine(ItemBankFolderPath, saveToDelete), true);
        }
    }

    private void RunCharacterGarbageCollector()
    {
        List<string> names = [];
        foreach (var file in Directory.EnumerateFiles(Path.Combine(CharacterFolderPath, SanitizedPlayerName)))
        {
            names.Add(Path.GetFileName(file));
        }

        names.Remove("_latest");
        names.Sort();

        while (names.Count > _saveCountToKeep)
        {
            var saveToDelete = names[0];
            names.RemoveAt(0);

            var targetPath = Path.Combine(CharacterFolderPath, SanitizedPlayerName, saveToDelete);

            // Failsafe
            if (!targetPath.Contains(ModDataFolderName))
                throw new InvalidOperationException($"Got an invalid file to delete {targetPath}, please notify the mod developer!");

            File.Delete(targetPath);
        }
    }

    private void AutosaveCurrentItemBank()
    {
        try
        {
            Directory.CreateDirectory(ModDataFolderPath);
            var itembankFolder = Path.Combine(ItemBankFolderPath, SanitizedCurrentTime);
            Directory.CreateDirectory(itembankFolder);

            ItemBankAutoSaver.TrySaveCurrentProfileToLocation(itembankFolder);

            if (ItemBankAutoSaver.SaveDone)
            {
                var latestSave = Path.Combine(ItemBankFolderPath, "_latest");
                Directory.CreateDirectory(latestSave);

                foreach (var path in Directory.EnumerateFiles(itembankFolder))
                {
                    File.Copy(path, Path.Combine(latestSave, Path.GetFileName(path)), true);
                }
            }

            RunItemBankGarbageCollector();
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to autosave!");
            Logger.LogError($"Exception message: {e}");
            return;
        }
    }

    private void AutosaveCurrentCharacter()
    {
        try
        {
            if (!(bool)Player._mainPlayer)
            {
                Logger.LogError($"Couldn't autosave! No main player found.");
                return;
            }

            Directory.CreateDirectory(ModDataFolderPath);
            var characterFolder = Path.Combine(CharacterFolderPath, SanitizedPlayerName);
            Directory.CreateDirectory(characterFolder);

            var filePath = Path.Combine(characterFolder, SanitizedCurrentTime);

            CharacterAutoSaver.TrySaveCurrentProfileToLocation(filePath);

            if (CharacterAutoSaver.SaveDone)
            {
                File.Copy(filePath, Path.Combine(characterFolder, "_latest"), true);
            }

            RunCharacterGarbageCollector();
        }
        catch (Exception e)
        {
            Logger.LogError($"Couldn't autosave!");
            Logger.LogError($"Exception message: {e}");
            return;
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnPlayerMapInstanceChange))]
static class Player_OnPlayerMapInstanceChange
{
    static void Postfix(Player __instance, MapInstance _new)
    {
        if (__instance == Player._mainPlayer)
        {
            AutoSaverMod.Instance.GameEntered();
        }
    }
}

[HarmonyPatch(typeof(InGameUI), nameof(InGameUI.Init_DisconnectGame))]
static class InGameUI_Init_DisconnectGame
{
    static void Prefix()
    {
        AutoSaverMod.Instance.GameExited();
    }
}

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Save_ItemStorageData))]
static class ItemBankAutoSaver
{
    public static void TrySaveCurrentProfileToLocation(string location)
    {
        if (SaveDone)
        {
            AutoSaverMod.Instance.Logger.LogInfo("Triggering item bank save process...");
        }

        SaveLocationOverride = location;
        BanksDone = 0;
        SaveDone = false;
        SaveProfileData(ProfileDataManager._current);
    }

    private static string SaveLocationOverride; // Directory
    private static string TempContents;

    private static int BanksDone = 0;
    private static int BanksMax = 0;
    internal static bool SaveDone { get; private set; } = true;

    [HarmonyReversePatch]
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
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => MarkSaveDone(null))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(TempContents)))
                    );

                matcher.Advance(1);
            }

            const int expectedLocations = 3;

            if (patchLocations != expectedLocations)
            {
                AutoSaverMod.Instance.Logger.LogWarning($"WARNING: Expected {expectedLocations} patch locations, got {patchLocations}.");
                AutoSaverMod.Instance.Logger.LogWarning($"The mod might behave incorrectly as a result. You should tell the mod developer about this.");
            }

            BanksMax = expectedLocations;

            return matcher.InstructionEnumeration();
        }

        _ = Transpiler(null);
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
}

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Save_ProfileData))]
static class CharacterAutoSaver
{
    public static void TrySaveCurrentProfileToLocation(string location)
    {
        if (SaveDone)
        {
            AutoSaverMod.Instance.Logger.LogInfo("Triggering character save process...");
        }

        SaveLocationOverride = location;
        SaveDone = false;
        SaveProfileData(ProfileDataManager._current);
    }

    private static string SaveLocationOverride;
    private static string TempContents;
    internal static bool SaveDone { get; private set; } = true;

    [HarmonyReversePatch]
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
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => MarkSaveDone(null))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CharacterAutoSaver), nameof(TempContents)))
                    );

                matcher.Advance(1);
            }

            const int expectedLocations = 3;

            if (patchLocations != expectedLocations)
            {
                AutoSaverMod.Instance.Logger.LogWarning($"WARNING: Expected {expectedLocations} patch locations, got {patchLocations}.");
                AutoSaverMod.Instance.Logger.LogWarning($"The mod might behave incorrectly as a result. You should tell the mod developer about this.");
            }

            return matcher.InstructionEnumeration();
        }

        _ = Transpiler(null);
        throw new NotImplementedException("Stub method");
    }

    private static string MarkSaveDone(string location)
    {
        SaveDone = true;
        return location;
    }
}