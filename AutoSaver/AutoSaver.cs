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
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Marioalexsan.AutoSaver.HarmonyReversePatches;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.AutoSaver;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class AutoSaverMod : BaseUnityPlugin
{
    public const string MoreBankTabsIndentifier = "com.16mb.morebanktabs";

    public new ManualLogSource Logger { get; private set; }

    public static AutoSaverMod Instance { get; private set; }

    private Harmony _harmony;
    private TimeSpan _elapsedTime;

    public bool SaveOnMapChange { get; private set; }
    public bool CharacterActive { get; private set; }
    public bool AutosaveActive => CharacterActive;
    public bool EnableExperimentalFeatures { get; private set; }
    public KeyCode SaveMultiplayerKeyCode { get; private set; }

    private readonly char[] BannedChars = [.. Path.GetInvalidPathChars(), .. Path.GetInvalidFileNameChars()];

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        Logger.LogInfo("AutoSaver patching...");
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        Logger.LogInfo("AutoSaver configuring...");
        Configure();

        Logger.LogInfo("AutoSaver initialized!");
    }

    private TimeSpan _autosaveInterval;
    private int _saveCountToKeep;

    public bool DetectedMoreBankTabsMod { get; private set; } = false;

    private void Configure()
    {
        int autosaveMinutes = Config.Bind("General", "BackupInterval", 4, "Interval between save backups, in minutes (min 1, max 60).").Value;

        autosaveMinutes = Math.Max(Math.Min(autosaveMinutes, 60), 1);
        _autosaveInterval = TimeSpan.FromMinutes(autosaveMinutes);

        _saveCountToKeep = Config.Bind("General", "SavesToKeep", 15, "Maximum number of saves to keep (min 5, max 50).").Value;
        _saveCountToKeep = Math.Max(Math.Min(_saveCountToKeep, 50), 5);

        SaveOnMapChange = Config.Bind("General", "SaveOnMapChange", false, "Set to true to trigger autosaving whenever a new level is loaded.").Value;

        EnableExperimentalFeatures = Config.Bind("Experimental", "EnableExperimentalFeatures", false, "Set to true to enable experimental features.").Value;
        SaveMultiplayerKeyCode = Config.Bind("Experimental", "SaveMultiplayerKeyCode", KeyCode.KeypadPlus, $"Key to trigger saving other people's saves in multiplayer under the \"Multi\" folder.{Environment.NewLine}Note that saves saved in this way are lackluster.").Value;
    }

    internal void GameEntered()
    {
        Logger.LogInfo("Game entered / map switched. Activating character autosaves.");
        ProfileDataManager._current.Load_ItemStorageData(); // This isn't loaded on start for some reason
        RunAutosaves();
        _elapsedTime = TimeSpan.Zero;
        CharacterActive = true;
    }

    internal void GameExited()
    {
        Logger.LogInfo("Game exited. Stopping character autosaves.");
        RunAutosaves();
        _elapsedTime = TimeSpan.Zero;
        CharacterActive = false;
    }

    private void Update()
    {
        if (!DetectedMoreBankTabsMod)
        {
            if (Chainloader.PluginInfos.ContainsKey(MoreBankTabsIndentifier))
            {
                DetectedMoreBankTabsMod = true;
                Logger.LogInfo($"Detected MoreBankTabs mod ({MoreBankTabsIndentifier}).");
                Logger.LogInfo($"Will try to backup the extra bank tabs.");
            }
        }

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

        if (EnableExperimentalFeatures && Input.GetKeyDown(SaveMultiplayerKeyCode))
        {
            Logger.LogInfo("[Experimental] Saving every online player's saves.");
            
            foreach (var player in FindObjectsOfType<Player>())
            {
                Logger.LogInfo($"Saving player data for {player._nickname}");
                Directory.CreateDirectory(MultiSavesFolderPath);
                CharacterAutoSaver.TrySaveSpecificProfileToLocation(player, Path.Combine(MultiSavesFolderPath, SanitizePlayerName(player)));

                if (!CharacterAutoSaver.SaveDone)
                {
                    Logger.LogWarning($"Failed to do save for {player._nickname}. Current game status is {player._currentGameCondition}.");
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

    public string SanitizePlayerName(Player player)
    {
        var playerName = player._nickname;

        for (int i = 0; i < BannedChars.Length; i++)
        {
            playerName = playerName.Replace($"{BannedChars[i]}", $"_{i}");
        }

        return playerName;
    }

    public string GetBackupNameForCurrentPlayer()
    {
        return $"{SanitizePlayerName(Player._mainPlayer)}_slot{ProfileDataManager._current._selectedFileIndex}";
    }

    private readonly string ModDataFolderName = "Marioalexsan_AutoSaver";
    private string ModDataFolderPath => Path.Combine(ProfileDataManager._current._dataPath, ModDataFolderName);
    private string CharacterFolderPath => Path.Combine(ModDataFolderPath, "Characters");
    private string ItemBankFolderPath => Path.Combine(ModDataFolderPath, "ItemBank");
    private string MultiSavesFolderPath => Path.Combine(ModDataFolderPath, "Multi");

    private void RunItemBankGarbageCollector()
    {
        if (!Directory.Exists(ItemBankFolderPath))
        {
            Logger.LogWarning($"Couldn't find backup path {ItemBankFolderPath} to run garbage collection for.");
            return;
        }

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
        var backupPath = Path.Combine(CharacterFolderPath, GetBackupNameForCurrentPlayer());

        if (!Directory.Exists(backupPath))
        {
            Logger.LogWarning($"Couldn't find backup path {backupPath} to run garbage collection for.");
            return;
        }    

        List<string> names = [];
        foreach (var file in Directory.EnumerateFiles(backupPath))
        {
            names.Add(Path.GetFileName(file));
        }

        names.Remove("_latest");
        names.Sort();

        while (names.Count > _saveCountToKeep)
        {
            var saveToDelete = names[0];
            names.RemoveAt(0);

            var targetPath = Path.Combine(CharacterFolderPath, GetBackupNameForCurrentPlayer(), saveToDelete);

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
                if (DetectedMoreBankTabsMod)
                    ItemBankAutoSaver.SaveModBankTabsToLocation(itembankFolder);

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
            var characterFolder = Path.Combine(CharacterFolderPath, GetBackupNameForCurrentPlayer());
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
        if (__instance == Player._mainPlayer && (AutoSaverMod.Instance.SaveOnMapChange || !AutoSaverMod.Instance.CharacterActive))
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
