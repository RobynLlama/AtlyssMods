using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Marioalexsan.ModAudio.SoftDependencies;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Marioalexsan.ModAudio;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
[BepInDependency(EasySettings.ModID, BepInDependency.DependencyFlags.SoftDependency)]
public class ModAudio : BaseUnityPlugin
{
    public static ModAudio Plugin { get; private set; }

    public const float MinWeight = 0.001f;
    public const float MaxWeight = 1000f;
    public const float DefaultWeight = 1f;
    public const string DefaultClipIdentifier = "___default___";

    internal new ManualLogSource Logger { get; private set; }

    private Harmony _harmony;

    private bool _reloadRequired = true;

    public string ModAudioConfigFolder => Path.Combine(Paths.ConfigPath, $"{ModInfo.PLUGIN_GUID}_UserAudioPack");
    public string ModAudioPluginFolder => Path.GetDirectoryName(Info.Location);

    private void SetupBaseAudioPack()
    {
        if (!Directory.Exists(ModAudioConfigFolder))
            Directory.CreateDirectory(ModAudioConfigFolder);

        var modConfigPath = Path.Combine(ModAudioConfigFolder, AudioPackLoader.AudioPackConfigName);
        var routesPath = Path.Combine(ModAudioConfigFolder, AudioPackLoader.RoutesConfigName);

        if (!File.Exists(modConfigPath) && !File.Exists(routesPath))
        {
            var modConfig = new AudioPackConfig
            {
                UniqueId = ModInfo.PLUGIN_GUID,
                DisplayName = ModInfo.PLUGIN_NAME,
            };

            File.WriteAllText(modConfigPath, JsonConvert.SerializeObject(modConfig));
            VanillaClipNames.GenerateReferenceFile(Path.Combine(ModAudioConfigFolder, "clip_names.txt"));
        }
    }

    private void Awake()
    {
        Plugin = this;
        Logger = base.Logger;

        _harmony = Harmony.CreateAndPatchAll(typeof(ModAudio).Assembly, ModInfo.PLUGIN_GUID);

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

        SetupBaseAudioPack();
        InitializeConfiguration();

        Logger.LogInfo("Initialized successfully!");
    }

    public ConfigEntry<bool> VerboseLoading { get; private set; }
    public ConfigEntry<bool> VerboseReplacements { get; private set; }
    public ConfigEntry<bool> OverrideCustomAudio { get; private set; }

    public Dictionary<string, ConfigEntry<bool>> AudioPackEnabled { get; } = [];

    private void InitializeConfiguration()
    {
        VerboseLoading = Config.Bind("Logging", nameof(VerboseLoading), false, Texts.VerboseLoadingDescription);
        VerboseReplacements = Config.Bind("Logging", nameof(VerboseReplacements), false, Texts.VerboseRoutingDescription);
        OverrideCustomAudio = Config.Bind("Logging", nameof(OverrideCustomAudio), false, Texts.OverrideCustomAudioDescription);

        if (EasySettings.IsAvailable)
        {
            EasySettings.OnApplySettings.AddListener(() =>
            {
                Config.Save();

                foreach (var pack in AudioEngine.AudioPacks)
                {
                    pack.Enabled = !AudioPackEnabled.TryGetValue(pack.Config.UniqueId, out var config) || config.Value;
                    Logger.LogInfo($"Pack {pack.Config.UniqueId} is now {pack.Enabled}");
                }

                AudioEngine.SoftReload();
            });
            EasySettings.OnInitialized.AddListener(() =>
            {
                EasySettings.AddHeader(ModInfo.PLUGIN_NAME);
                EasySettings.AddToggle(Texts.VerboseReplacemensTitle, VerboseReplacements);
                EasySettings.AddToggle(Texts.VerboseLoadingTitle, VerboseLoading);
                EasySettings.AddToggle(Texts.OverrideCustomAudioTitle, OverrideCustomAudio);
            });
        }
    }

    private void InitializePackConfiguration()
    {
        if (EasySettings.IsAvailable)
        {
            EasySettings.AddHeader($"{ModInfo.PLUGIN_NAME} audio packs");
            EasySettings.AddButton(Texts.ReloadTitle, () => _reloadRequired = true);

            foreach (var pack in AudioEngine.AudioPacks)
            {
                if (!AudioPackEnabled.ContainsKey(pack.Config.UniqueId))
                {
                    var enabled = Config.Bind("EnabledAudioPacks", pack.Config.UniqueId, true, Texts.EnablePackDescription(pack.Config.DisplayName));

                    AudioPackEnabled[pack.Config.UniqueId] = enabled;
                    EasySettings.AddToggle(pack.Config.DisplayName, enabled);
                    pack.Enabled = enabled.Value;
                }
            }
        }
    }

    private void CheckForObsoleteStuff()
    {
        static bool IsAudioPackFile(string name)
        {
            return
                Path.GetFileName(name) == AudioPackLoader.AudioPackConfigName
                || Path.GetFileName(name) == AudioPackLoader.RoutesConfigName
                || AudioClipLoader.SupportedLoadExtensions.Contains(Path.GetExtension(name));
        }

        var pluginPath = Path.GetDirectoryName(Info.Location);
        var audioPath = Path.Combine(pluginPath, "audio");

        var hasAudioFilesInPlugins = false;

        if (Directory.GetFiles(pluginPath).Any(IsAudioPackFile))
            hasAudioFilesInPlugins = true;

        if (Directory.Exists(audioPath) && Directory.GetFiles(audioPath).Any(IsAudioPackFile))
            hasAudioFilesInPlugins = true;

        if (hasAudioFilesInPlugins)
        {
            Logger.LogWarning("TODO OLD FILES IN PLUGINS!!!11111");
        }
    }

    private void Update()
    {
        if (_reloadRequired)
        {
            _reloadRequired = false;
            CheckForObsoleteStuff();

            InitializeConfiguration();
            AudioEngine.Reload();
            InitializePackConfiguration();
        }

        AudioEngine.Update();
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        StartCoroutine(CheckNewScene());
    }

    System.Collections.IEnumerator CheckNewScene()
    {
        yield return null; // A frame delay should allow all objects to actually load in (?)

        if (VerboseReplacements.Value)
            Logger.LogInfo("Checking new scene for audio sources...");

        foreach (var audio in FindObjectsOfType<AudioSource>(true))
        {
            AudioEngine.AudioPlayed(audio);
        }
    }
}