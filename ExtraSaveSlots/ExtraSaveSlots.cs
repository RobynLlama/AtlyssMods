using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Marioalexsan.ExtraSaveSlots;

// TODO cleanup all of this patch nonsense
[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Cache_CharacterFiles))]
static class ProfileDataManager_Cache_CharacterFiles
{
    static void Prefix()
    {
        ExtraSaveSlots.ResizeSlots();
    }
}

[HarmonyPatch(typeof(CharacterSelectManager), nameof(CharacterSelectManager.Initalize_CharacterFiles))]
static class ProfileDataManager_Initalize_CharacterFiles
{
    internal static void Prefix(ref CharacterFile[] _characterFiles)
    {
        ExtraSaveSlots.ResizeSlots();
        _characterFiles = ProfileDataManager._current._characterFiles;
    }
}

[HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.Create_Character))]
static class CharacterCreationManager_Create_Character
{
    static void Postfix()
    {
        ProfileDataManager._current._charSelectManager.Clear_SelectEntries();
        ProfileDataManager._current._charSelectManager.Initalize_CharacterFiles(ProfileDataManager._current._characterFiles);
    }
}

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Awake))]
static class ProfileDataManager_Awake
{
    internal static string SerializedEmptySaveFile = "";
    internal static int HighestSlotIndex = -1;

    // We need to save one of the premade "empty" character files to create new "empty" slots
    static void Postfix(ProfileDataManager __instance)
    {
        HighestSlotIndex = ExtraSaveSlots.FindHighestSaveSlotIndex();
        SerializedEmptySaveFile = JsonUtility.ToJson(__instance._characterFiles[0]);
    }
}

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Start))]
static class ProfileDataManager_Start
{
    // Converts the vanilla save slot panel to a scroll view variant
    // This happens on scene load effectively
    static void Prefix(ProfileDataManager __instance)
    {
        __instance._charSelectManager._characterSelectListContent = ExtraSaveSlots.ConvertToScrollView(__instance._charSelectManager._characterSelectListContent);
    }
}

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class ExtraSaveSlots : BaseUnityPlugin
{
    public ConfigEntry<int> MaxSupportedSaves { get; }

    public static int FindHighestSaveSlotIndex()
    {
        int maxIndex = -1;

        for (int i = 0; i < Plugin.MaxSupportedSaves.Value; i++)
        {
            if (File.Exists(Path.Combine(ProfileDataManager._current._dataPath, $"atl_characterProfile_{i}")))
                maxIndex = i;
        }

        return maxIndex;
    }

    internal static void ResizeSlots()
    {
        var previousLength = ProfileDataManager._current._characterFiles.Length;

        var newSlotNeeded = previousLength <= 6 || !ProfileDataManager._current._characterFiles[^1]._isEmptySlot;

        var requiredSaveSlotCount = Math.Min(Math.Max(ProfileDataManager_Awake.HighestSlotIndex + 2, previousLength + (newSlotNeeded ? 1 : 0)), Plugin.MaxSupportedSaves.Value);

        if (previousLength < requiredSaveSlotCount)
        {
            Array.Resize(ref ProfileDataManager._current._characterFiles, requiredSaveSlotCount);

            for (int i = previousLength; i < ProfileDataManager._current._characterFiles.Length; i++)
            {
                ProfileDataManager._current._characterFiles[i] = GetNewFile();
            }
        }
    }

    internal static Transform ConvertToScrollView(Transform container)
    {
        var eye = container.Find("_charSelect_illuminatiEye");

        var settingsTab = GameObject.Find("_dolly_networkSettingsTab");

        if (!settingsTab || !settingsTab.transform.Find("Image/Scroll View/Viewport/Content"))
        {
            Logging.LogWarning($"Couldn't instantiate scroll view! Wasn't able to find components to use!");
            return container;
        }

        var rootObj = settingsTab.transform.Find("Image");

        var obj = Instantiate(rootObj);
        obj.SetParent(null);

        var scrollView = obj.Find("Scroll View");
        var content = obj.Find("Scroll View/Viewport/Content");

        // Clear out existing junk
        for (int i = 0; i < content.transform.childCount; i++)
        {
            Destroy(content.transform.GetChild(i).gameObject);
        }

        // Transfer items from container
        while (container.childCount > 0)
        {
            container.GetChild(0).SetParent(content);
        }

        // Retransfer eye directly to the new area, it shouldn't be part of scroll view
        eye.SetParent(container);

        obj.transform.SetParent(container);

        var imageRect = obj.GetComponent<RectTransform>();
        var saveSlotsRect = scrollView.GetComponent<RectTransform>();
        var containerRect = container.GetComponent<RectTransform>();

        var eyeHeight = eye?.gameObject.GetComponent<RectTransform>().rect.height ?? 0;

        containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.rect.width + 40);

        imageRect.localScale = Vector3.one;
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.rect.width - 20);
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.rect.height - eyeHeight - 40);
        saveSlotsRect.localScale = Vector3.one;

        saveSlotsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerRect.rect.width - 20);
        saveSlotsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerRect.rect.height - eyeHeight - 40);

        return content;
    }

    public static ExtraSaveSlots Plugin => _plugin ?? throw new InvalidOperationException($"{nameof(ExtraSaveSlots)} hasn't been initialized yet. Either wait until initialization, or check via ChainLoader instead.");
    private static ExtraSaveSlots? _plugin;

    internal new ManualLogSource Logger { get; private set; }

    private readonly Harmony _harmony;

    public static CharacterFile GetNewFile()
    {
        var file = JsonUtility.FromJson<CharacterFile>(ProfileDataManager_Awake.SerializedEmptySaveFile);
        file._isEmptySlot = true; // Failsafe
        return file;
    }

    public ExtraSaveSlots()
    {
        _plugin = this;
        Logger = base.Logger;
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        MaxSupportedSaves = Config.Bind(
            "General",
            "MaxSupportedSaves",
            128,
            new ConfigDescription("Max number of saves to support. Don't change this unless you really, really need more saves than the default value provides.", new AcceptableValueRange<int>(128, 1024))
        );
    }

    private void Awake()
    {
        _harmony.PatchAll();
        Logger.LogInfo("Initialized successfully!");
    }
}