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
    public static readonly Version ExpectedVersion = new Version("1.1.3");

    public static bool IsAvailable
    {
        get
        {
            if (Plugin != null)
            {
                if (Plugin.Info.Metadata.Version != ExpectedVersion)
                    Logging.LogWarning($"EasySettings found, but its version ({Plugin.Info.Metadata.Version}) differs from the expected one ({ExpectedVersion}).");

                return true;
            }

            return false;
        }
    }

    public static UnityEvent OnInitialized => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnInitialized").Invoke(null, []);
    public static UnityEvent OnCancelSettings => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnCancelSettings").Invoke(null, []);
    public static UnityEvent OnApplySettings => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnApplySettings").Invoke(null, []);
    public static UnityEvent OnCloseSettings => (UnityEvent)AccessTools.PropertyGetter(SettingsType, "OnCloseSettings").Invoke(null, []);

    public static GameObject AddSpace() => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddSpace").Invoke(SettingsTab, []));

    public static GameObject AddHeader(string label) => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddHeader", [typeof(string)]).Invoke(SettingsTab, [label]));

    public static GameObject AddButton(string buttonLabel, UnityAction onClick) => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddButton", [typeof(string), typeof(UnityAction)]).Invoke(SettingsTab, [buttonLabel, onClick]));

    public static GameObject AddToggle(string label, ConfigEntry<bool> config) => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddToggle", [typeof(string), typeof(ConfigEntry<bool>)]).Invoke(SettingsTab, [label, config]));

    public static GameObject AddSlider(string label, ConfigEntry<float> config, bool wholeNumbers = false) => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddSlider", [typeof(string), typeof(ConfigEntry<float>), typeof(bool)]).Invoke(SettingsTab, [label, config, wholeNumbers]));

    public static GameObject AddAdvancedSlider(string label, ConfigEntry<float> config, bool wholeNumbers = false) => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddAdvancedSlider", [typeof(string), typeof(ConfigEntry<float>), typeof(bool)]).Invoke(SettingsTab, [label, config, wholeNumbers]));

    public static GameObject AddDropdown<T>(string label, ConfigEntry<T> config) where T : Enum => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddDropdown", [typeof(string), typeof(ConfigEntry<T>)]).MakeGenericMethod(typeof(T)).Invoke(SettingsTab, [label, config]));

    public static GameObject AddKeyButton(string label, ConfigEntry<KeyCode> config) => GetGameObject(AccessTools.Method(SettingsTab.GetType(), "AddKeyButton", [typeof(string), typeof(ConfigEntry<KeyCode>)]).Invoke(SettingsTab, [label, config]));

    // ==================================

    private static BaseUnityPlugin Plugin => Chainloader.PluginInfos.TryGetValue(ModID, out PluginInfo info) ? info.Instance : null;
    private static Type SettingsType => Plugin.GetType().Assembly.GetType("Nessie.ATLYSS.EasySettings.Settings");
    private static object SettingsTab => AccessTools.PropertyGetter(SettingsType, "ModTab").Invoke(null, []);

    private static GameObject GetGameObject(object baseElement) => ((RectTransform)AccessTools.Field(baseElement.GetType(), "Root").GetValue(baseElement)).gameObject;
}
