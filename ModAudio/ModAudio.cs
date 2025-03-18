using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Marioalexsan.ModAudio.HarmonyPatches;
using Marioalexsan.ModAudio.SoftDependencies;
using Mono.Cecil;
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

        VanillaClipNames.GenerateReferenceFile(Path.Combine(ModAudioConfigFolder, "clip_names.txt"));
        File.WriteAllText(Path.Combine(ModAudioConfigFolder, "schema_modaudio.config.json"), AudioPackConfig.GenerateSchema());
    }

    private void Awake()
    {
        Plugin = this;
        Logger = base.Logger;

        _harmony = Harmony.CreateAndPatchAll(typeof(ModAudio).Assembly, ModInfo.PLUGIN_GUID);

        SetupBaseAudioPack();
        InitializeConfiguration();
    }

    public ConfigEntry<bool> LogPackLoading { get; private set; }
    public ConfigEntry<bool> LogAudioPlayed { get; private set; }

    public ConfigEntry<bool> UseMaxDistanceForLogging { get; private set; }
    public ConfigEntry<float> MaxDistanceForLogging { get; private set; }

    public ConfigEntry<bool> LogAmbience { get; private set; }
    public ConfigEntry<bool> LogGame { get; private set; }
    public ConfigEntry<bool> LogGUI { get; private set; }
    public ConfigEntry<bool> LogMusic { get; private set; }
    public ConfigEntry<bool> LogVoice { get; private set; }

    public Dictionary<string, ConfigEntry<bool>> AudioPackEnabled { get; } = [];
    public Dictionary<string, GameObject> AudioPackEnabledObjects { get; } = [];

    public GameObject AudioPackEnabledRoot { get; set; }

    private void InitializeConfiguration()
    {
        LogPackLoading = Config.Bind("Logging", nameof(LogPackLoading), true, Texts.LogAudioLoadingDescription);
        LogAudioPlayed = Config.Bind("Logging", nameof(LogAudioPlayed), false, Texts.LogAudioPlayedDescription);
        UseMaxDistanceForLogging = Config.Bind("Logging", nameof(UseMaxDistanceForLogging), false, Texts.UseMaxDistanceForLoggingDescription);
        MaxDistanceForLogging = Config.Bind("Logging", nameof(MaxDistanceForLogging), 32f, new ConfigDescription(Texts.MaxDistanceForLoggingDescription, new AcceptableValueRange<float>(32f, 2048)));

        LogAmbience = Config.Bind("Logging", nameof(LogAmbience), true, Texts.LogAmbienceDescription);
        LogGame = Config.Bind("Logging", nameof(LogGame), true, Texts.LogGameDescription);
        LogGUI = Config.Bind("Logging", nameof(LogGUI), true, Texts.LogGUIDescription);
        LogMusic = Config.Bind("Logging", nameof(LogMusic), true, Texts.LogMusicDescription);
        LogVoice = Config.Bind("Logging", nameof(LogVoice), true, Texts.LogVoiceDescription);

        if (EasySettings.IsAvailable)
        {
            EasySettings.OnApplySettings.AddListener(() =>
            {
                try
                {
                    Config.Save();

                    bool softReloadRequired = false;

                    foreach (var pack in AudioEngine.AudioPacks)
                    {
                        var enabled = !AudioPackEnabled.TryGetValue(pack.Config.Id, out var config) || config.Value;

                        if (enabled != pack.Enabled)
                        {
                            Logger.LogInfo($"Pack {pack.Config.Id} is now {(enabled ? "enabled" : "disabled")}");
                            softReloadRequired = true;
                        }


                        pack.Enabled = enabled;
                    }

                    if (softReloadRequired)
                        AudioEngine.SoftReload();
                }
                catch (Exception e)
                {
                    Logging.LogError($"ModAudio crashed in OnApplySettings! Please report this error to the mod developer:");
                    Logging.LogError(e.ToString());
                }
            });
            EasySettings.OnInitialized.AddListener(() =>
            {
                EasySettings.AddHeader(ModInfo.PLUGIN_NAME);
                EasySettings.AddToggle(Texts.LogAudioLoadingTitle, LogPackLoading);
                EasySettings.AddToggle(Texts.LogAudioPlayedTitle, LogAudioPlayed);
                EasySettings.AddToggle(Texts.UseMaxDistanceForLoggingTitle, UseMaxDistanceForLogging);
                EasySettings.AddAdvancedSlider(Texts.MaxDistanceForLoggingTitle, MaxDistanceForLogging, true);

                EasySettings.AddToggle(Texts.LogAmbienceTitle, LogAmbience);
                EasySettings.AddToggle(Texts.LogGameTitle, LogGame);
                EasySettings.AddToggle(Texts.LogGUITitle, LogGUI);
                EasySettings.AddToggle(Texts.LogMusicTitle, LogMusic);
                EasySettings.AddToggle(Texts.LogVoiceTitle, LogVoice);
            });
        }
    }

    internal void InitializePackConfiguration()
    {
        if (!AudioPackEnabledRoot)
        {
            if (EasySettings.IsAvailable)
            {
                EasySettings.AddHeader($"{ModInfo.PLUGIN_NAME} audio packs");
                AudioPackEnabledRoot = EasySettings.AddButton(Texts.ReloadTitle, () => _reloadRequired = true);
            }
        }

        foreach (var pack in AudioEngine.AudioPacks)
        {
            if (!AudioPackEnabled.TryGetValue(pack.Config.Id, out var existingEntry))
            {
                var enabled = Config.Bind("EnabledAudioPacks", pack.Config.Id, true, Texts.EnablePackDescription(pack.Config.DisplayName));

                AudioPackEnabled[pack.Config.Id] = enabled;

                if (EasySettings.IsAvailable)
                {
                    if (!AudioPackEnabledObjects.ContainsKey(pack.Config.Id))
                        AudioPackEnabledObjects[pack.Config.Id] = EasySettings.AddToggle(pack.Config.DisplayName, enabled);
                }

                pack.Enabled = enabled.Value;
            }
            else
            {
                pack.Enabled = existingEntry.Value;
            }
        }

        if (EasySettings.IsAvailable)
        {
            int siblingIndex = AudioPackEnabledRoot.transform.GetSiblingIndex() + 1;

            foreach (var config in AudioPackEnabledObjects.OrderBy(x => x.Key))
            {
                // Reorder so that it's in the audio pack list
                config.Value.transform.SetSiblingIndex(siblingIndex++);
            }

            foreach (var config in AudioPackEnabledObjects)
            {
                // Show or hide if pack is present
                config.Value.SetActive(AudioEngine.AudioPacks.Any(x => x.Config.Id == config.Key));
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
            Logger.LogWarning("There is an audio pack under ModAudio's plugin folder!");
            Logger.LogWarning("Please use the folder from BepInEx/config for your custom packs instead of the plugin folder.");
            Logger.LogWarning("When using r2modman, the plugin folder might be deleted mercilessly when you update or uninstall your mod, which will delete your custom audio.");
        }
    }

    private void Update()
    {
        try
        {
            if (_reloadRequired)
            {
                _reloadRequired = false;
                CheckForObsoleteStuff();

                AudioEngine.HardReload();
            }

            AudioEngine.Update();
        }
        catch (Exception e)
        {
            Logging.LogError($"ModAudio crashed in {nameof(Update)}! Please report this error to the mod developer:");
            Logging.LogError(e.ToString());
        }
    }
}