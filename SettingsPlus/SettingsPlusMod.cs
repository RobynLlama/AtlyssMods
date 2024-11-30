using System;
using System.IO;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.SettingsPlus;

public class ModSettingsProfile
{
    public int _fps = 60;
}

[BepInPlugin("Marioalexsan.SettingsPlus", "SettingsPlus", "1.0.0")]
public class SettingsPlusMod : BaseUnityPlugin
{
    internal new ManualLogSource Logger;
    internal static SettingsPlusMod Instance;

    private static ModSettingsProfile _settings = new();
    private static bool _initedMenu = false;

    private Harmony _harmony;

    private Slider _fpsSlider;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        FPS = 60;
        VSync = false;

        _harmony = new Harmony("Marioalexsan.SettingsPlus");
        _harmony.PatchAll();

        Logger.LogInfo("Hello from Marioalexsasn.SettingsPlus!");
    }

    internal void InitMenu(SettingsManager settings)
    {
        if (_initedMenu)
            return;

        _initedMenu = true;

        Logger.LogInfo("Editing settings menu.");
        var list = GameObject.Find("_dolly_videoSettingsTab").transform.Find("_backdrop_videoSettings/Scroll View/Viewport/Content");
        var vSync = list.transform.Find("_cell_verticalSync");

        var fpsSetting = GameObject.Instantiate(list.transform.Find("_cell_fieldOfView"));
        fpsSetting.name = "Marioalexsan_Settings_FPSSlider";

        var counter = fpsSetting.transform.Find("_text_fovCounter").GetComponent<Text>();
        counter.text = $"{FPS}";

        var slider = _fpsSlider = fpsSetting.GetComponentInChildren<Slider>();
        slider.minValue = 30;
        slider.maxValue = 240;
        slider.value = FPS;
        slider.wholeNumbers = true;
        slider.onValueChanged.AddListener((value) =>
        {
            counter.text = $"{value}";
        });

        var button = fpsSetting.GetComponentInChildren<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            slider.value = 60;
        });

        var text = fpsSetting.transform.Find("Text").GetComponent<Text>();
        text.text = "FPS";

        fpsSetting.transform.SetParent(list.transform);
        fpsSetting.transform.SetSiblingIndex(vSync.transform.GetSiblingIndex());
        fpsSetting.transform.localScale = Vector3.one;

        var vsyncSetting = list.transform.Find("_cell_verticalSync");

        var vsyncText = vsyncSetting.transform.Find("Text").GetComponent<Text>();
        vsyncText.text = "Vertical Sync (lock FPS)";

        Logger.LogInfo("Done.");
    }

    public int FPS
    {
        get => Application.targetFrameRate;
        set => Application.targetFrameRate = Math.Clamp(value, 30, 240);
    }

    public bool VSync
    {
        get => QualitySettings.vSyncCount == 1;
        set => QualitySettings.vSyncCount = true ? 1 : -1;
    }

    internal void LoadData(SettingsManager settings)
    {
        if (!File.Exists(settings._dataPath + "/settings_Marioalexsan_SettingsPlus.json"))
        {
            SaveData(settings);
            return;
        }

        string json = File.ReadAllText(settings._dataPath + "/settings_Marioalexsan_SettingsPlus.json");
        _settings = JsonUtility.FromJson<ModSettingsProfile>(json);

        if (_fpsSlider)
            _fpsSlider.value = _settings._fps;

        FPS = _settings._fps;
    }

    internal void SaveData(SettingsManager settings)
    {
        _settings._fps = (int)_fpsSlider.value;

        string contents = JsonUtility.ToJson(_settings, prettyPrint: true);
        File.WriteAllText(settings._dataPath + "/settings_Marioalexsan_SettingsPlus.json", contents);
    }
}