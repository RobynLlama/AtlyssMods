using BepInEx;
using UnityEngine.Events;
using UnityEngine;
using BepInEx.Bootstrap;
using HarmonyLib;
using BepInEx.Configuration;

namespace Marioalexsan.ModAudio.SoftDependencies;

public static class EasySettings
{
    public const string ModID = "EasySettings";

    public static bool IsAvailable => Plugin != null;

    public static UnityEvent OnInitialized => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnInitialized").Invoke(null, []);
    public static UnityEvent OnCancelSettings => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnCancelSettings").Invoke(null, []);
    public static UnityEvent OnApplySettings => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnApplySettings").Invoke(null, []);
    public static UnityEvent OnCloseSettings => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnCloseSettings").Invoke(null, []);

    public static void AddSpace() => AccessTools.Method(SettingsTab.GetType(), "AddSpace").Invoke(SettingsTab, []);

    public static void AddHeader(string label) => AccessTools.Method(SettingsTab.GetType(), "AddHeader", [typeof(string)]).Invoke(SettingsTab, [label]);

    public static void AddButton(string buttonLabel, UnityAction onClick) => AccessTools.Method(SettingsTab.GetType(), "AddButton", [typeof(string), typeof(UnityAction)]).Invoke(SettingsTab, [buttonLabel, onClick]);

    public static void AddToggle(string label, ConfigEntry<bool> config) => AccessTools.Method(SettingsTab.GetType(), "AddToggle", [typeof(string), typeof(ConfigEntry<bool>)]).Invoke(SettingsTab, [label, config]);

    public static void AddSlider(string label, ConfigEntry<bool> config, bool wholeNumbers = false) => AccessTools.Method(SettingsTab.GetType(), "AddSlider", [typeof(string), typeof(ConfigEntry<bool>), typeof(bool)]).Invoke(SettingsTab, [label, config, wholeNumbers]);

    public static void AddAdvancedSlider(string label, ConfigEntry<bool> config, bool wholeNumbers = false) => AccessTools.Method(SettingsTab.GetType(), "AddAdvancedSlider", [typeof(string), typeof(ConfigEntry<bool>), typeof(bool)]).Invoke(SettingsTab, [label, config, wholeNumbers]);

    public static void AddDropdown<T>(string label, ConfigEntry<T> config) where T : Enum => AccessTools.Method(SettingsTab.GetType(), "AddDropdown", [typeof(string), typeof(ConfigEntry<T>)]).MakeGenericMethod(typeof(T)).Invoke(SettingsTab, [label, config]);

    public static void AddKeyButton(string label, ConfigEntry<KeyCode> config) => AccessTools.Method(SettingsTab.GetType(), "AddKeyButton", [typeof(string), typeof(ConfigEntry<KeyCode>)]).Invoke(SettingsTab, [label, config]);

    // ==================================

    private static BaseUnityPlugin Plugin => Chainloader.PluginInfos.TryGetValue(ModID, out PluginInfo info) ? info.Instance : null;
    private static Type SettingsType => Plugin.GetType().Assembly.GetType("Nessie.ATLYSS.EasySettings.Settings");
    private static object SettingsTab => AccessTools.PropertyGetter(SettingsType, "ModTab").Invoke(null, []);
}
