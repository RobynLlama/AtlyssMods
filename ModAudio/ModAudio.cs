using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.ModAudio;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class ModAudio : BaseUnityPlugin
{
    private const float MinWeight = 0.001f;
    private const float MaxWeight = 1000f;
    internal const float DefaultWeight = 1f;
    private const string DefaultClipIdentifier = "___default___";

    private static readonly string[] AudioExtensions = [
        ".aiff",
        ".aif",
        ".mp3",
        ".ogg",
        ".wav",
        ".aac",
        ".alac"
    ];

    public static ModAudio Instance { get; private set; }
    internal new ManualLogSource Logger { get; private set; }

    private Harmony _harmony;
    private readonly ConditionalWeakTable<AudioSource, WeakReference<AudioClip>> _originalSources = new();

    private bool _ranAssetModsDetection;
    private List<string> _modFoldersChecked = [];
    private readonly List<(AudioClip, float)> _staticWeights = new List<(AudioClip, float)>(256); // Reuse to reduce amount of garbage generated
    private readonly System.Random _randomSource = new();

    private readonly Dictionary<string, Dictionary<string, AudioClip>> _customAudio = [];
    private readonly Dictionary<string, RouteConfig> _customRoutes = [];

    private string AudioLocation => Path.Combine(Path.GetDirectoryName(Info.Location), "audio");
    private string AudioReference => Path.Combine(AudioLocation, "__README.txt");
    private string AudioRoutes => Path.Combine(AudioLocation, "__routes.txt");

    public bool DetailedLogging { get; private set; }
    public bool OverrideCustomAudio { get; private set; }

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        Logger.LogInfo("Patching methods...");
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        Directory.CreateDirectory(AudioLocation);

        if (!File.Exists(AudioRoutes))
        {
            using var stream = File.Create(AudioRoutes);
        }

        DetailedLogging = Config.Bind("General", "ExtensiveLogging", false, "Set to true to enable detailed audio logging. Might be resource intensive / spammy").Value;
        OverrideCustomAudio = Config.Bind("General", "OverrideCustomAudio", false, "Set to true to have ModAudio's audio clips override any custom audio from other mods. If false, it will get mixed in with other mods instead.").Value;

        VanillaClipNames.GenerateReferenceFile(AudioReference);

        Logger.LogInfo("Loading custom audio...");
        LoadAudio(this, AudioLocation);

        Logger.LogInfo("Initialized successfully!");
    }

    private void DetectAssetMods()
    {
        try
        {
            foreach (var folder in Directory.GetDirectories(Paths.PluginPath))
            {
                // Load stuff from root folder if there are known clip names in it, or a __routes.txt file
                var rootRoutes = Path.Combine(folder, "__routes.txt");
                var rootFolder = Path.GetDirectoryName(rootRoutes);

                if (!_modFoldersChecked.Contains(Path.GetDirectoryName(rootRoutes)))
                {
                    if (File.Exists(rootRoutes) || Directory.GetFiles(rootFolder).Any(x => VanillaClipNames.IsKnownClip(Path.GetFileNameWithoutExtension(x))))
                        LoadAudioFromLocation(rootFolder, "ModAudio:path//" + rootRoutes);
                }

                // Add stuff from the audio folder if it exists
                var audioRoutes = Path.Combine(folder, "audio", "__routes.txt");
                var audioFolder = Path.GetDirectoryName(audioRoutes);

                if (!_modFoldersChecked.Contains(audioFolder))
                {
                    if (Directory.Exists(audioFolder))
                        LoadAudioFromLocation(audioFolder, "ModAudio:path//" + audioRoutes);
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning("Exception occurred while trying to detect asset mods.");
            Logger.LogWarning(e);

        }
    }

    private void Update()
    {
        if (!_ranAssetModsDetection)
        {
            _ranAssetModsDetection = true;
            DetectAssetMods();
        }

        //try
        //{
        //    if (Input.GetKeyDown(DebugKeybind))
        //    {
        //        DebugDisplayActive = !DebugDisplayActive;
        //        _debugDisplay.SetActive(DebugDisplayActive);
        //    }
        //}
        //catch (Exception e)
        //{
        //    Logger.LogError(e);
        //}
    }

    public void LoadModAudio(BaseUnityPlugin plugin, string audioFolder = null)
    {
        if (plugin == this)
            return;

        LoadAudio(plugin, audioFolder);
    }

    private void LoadAudio(BaseUnityPlugin plugin, string audioFolder)
    {
        audioFolder ??= Path.Combine(Path.GetDirectoryName(plugin.Info.Location), "audio");

        LoadAudioFromLocation(audioFolder, plugin.Info.Metadata.GUID);
    }

    private void LoadAudioFromLocation(string audioFolder, string id)
    {
        bool usedPath = id.StartsWith("ModAudio:path//");
        string idClean = id.Replace("ModAudio:path//", "").Replace(Paths.PluginPath, "");

        if (!_customAudio.TryGetValue(id, out var audio))
            audio = _customAudio[id] = [];

        if (!_customRoutes.TryGetValue(id, out var routes))
            routes = _customRoutes[id] = new();

        foreach (var file in Directory.GetFiles(audioFolder))
        {
            if (!AudioExtensions.Any(file.EndsWith))
                continue;

            var name = Path.GetFileNameWithoutExtension(file);

            if (audio.ContainsKey(name))
                return;

            if (DetailedLogging)
                Logger.LogInfo($"Loading {name} from {file}...");

            try
            {
                var clip = ClipLoader.LoadFromFile(name, file);
                audio[name] = clip;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to load {name} from {file}!");
                Logger.LogWarning($"Exception: {e}");
            }
        }

        var routesPath = Path.Combine(audioFolder, "__routes.txt");

        if (File.Exists(routesPath))
        {
            routes = _customRoutes[id] = RouteConfig.ReadTextFormat(routesPath);

            foreach (var replacements in routes.ReplacedClips)
            {
                foreach (var replacement in replacements.Value)
                {
                    var randomWeight = replacement.RandomWeight;

                    if (randomWeight < MinWeight)
                    {
                        Logger.LogWarning($"Weight {randomWeight} for {replacements.Value} => {replacement.Name} in {(usedPath ? "path" : "mod")} {id} is too low and was capped to {MinWeight}.");
                        randomWeight = MinWeight;
                    }

                    if (randomWeight > MaxWeight)
                    {
                        Logger.LogWarning($"Weight {randomWeight} for {replacements.Value} => {replacement.Name} {(usedPath ? "path" : "mod")} {id} is too high and was capped to {MaxWeight}.");
                        randomWeight = MaxWeight;
                    }

                    replacement.RandomWeight = randomWeight;
                }
            }
        }

        _modFoldersChecked.Add(Path.GetDirectoryName(routesPath));

        if (audio.Count == 0 && routes.IsEmpty())
        {
            _customAudio.Remove(id);
            _customRoutes.Remove(id);
        }
        else
        {
            Logger.LogInfo($"Loaded audio from {(usedPath ? "path" : "mod")} {idClean} ({audio.Count} clips, {routes.ReplacedClips.Count} routes, {routes.ReplacedClips.Select(x => x.Value).Sum(x => x.Count)} replacements).");
        }
    }

    public void Reroute(AudioSource input)
    {
        var originalClip = input.clip;

        if (_originalSources.TryGetValue(input, out var reference))
        {
            if (!reference.TryGetTarget(out originalClip))
            {
                _originalSources.Remove(input);
            }
        }

        if (Resolve(originalClip, out var destination))
        {
            if (DetailedLogging)
                Logger.LogInfo($"Rerouted \"{originalClip.name}\" => \"{destination.name}\"");

            if (input.clip.name == destination.name)
                return; // Same clip in audio source at the moment, don't need to do operations on it

            if (!_originalSources.TryGetValue(input, out _))
                _originalSources.Add(input, new WeakReference<AudioClip>(originalClip)); // Save the original clip if this is the first time we route it

            // AudioSource needs to be restarted for the new clip to be applied
            var wasPlaying = input.isPlaying;
            input.Stop();

            input.clip = destination;

            if (wasPlaying)
                input.Play();
        }
    }

    internal bool Resolve(AudioClip source, out AudioClip destination)
    {
        if (source?.name == null)
        {
            destination = null;
            return false;
        }

        // Clear just in case there's leftover garbage
        _staticWeights.Clear();

        if (_customAudio.ContainsKey(ModInfo.PLUGIN_GUID))
        {
            AddModWeights(source, ModInfo.PLUGIN_GUID);
        }

        if (!(OverrideCustomAudio && _staticWeights.Count > 0))
        {
            foreach (var guid in _customAudio.Keys)
            {
                if (guid == ModInfo.PLUGIN_GUID)
                    continue;

                AddModWeights(source, guid);
            }
        }

        if (_staticWeights.Count == 0)
        {
            destination = null;
            return false;
        }

        float totalWeight = _staticWeights.Sum(x => x.Item2);
        float randomValue = (float)(_randomSource.NextDouble() * totalWeight);

        int index = 0;
        AudioClip selectedClip = source; // Use source as default if rolling doesn't succeed for some reason

        while (randomValue > 0 && index < _staticWeights.Count)
        {
            selectedClip = _staticWeights[index].Item1;
            randomValue -= _staticWeights[index].Item2;
            index++;
        }

        // Clear to remove references
        _staticWeights.Clear();
        destination = selectedClip;
        return true;
    }

    private void AddModWeights(AudioClip source, string id)
    {
        if (_customAudio[id].TryGetValue(source.name, out var destination))
        {
            _staticWeights.Add((destination, DefaultWeight));
        }

        if (_customRoutes[id].ReplacedClips.TryGetValue(source.name, out var replacements))
        {
            foreach (var replacement in replacements)
            {
                if (replacement.Name == DefaultClipIdentifier)
                {
                    // Special case to allow playing the default audio with a chance
                    _staticWeights.Add((source, replacement.RandomWeight));
                }

                else if (_customAudio[id].TryGetValue(replacement.Name, out destination))
                {
                    _staticWeights.Add((destination, replacement.RandomWeight));
                }
            }
        }
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        StartCoroutine(CheckNewScene(arg0));
    }

    System.Collections.IEnumerator CheckNewScene(Scene arg0)
    {
        yield return null; // A frame delay should allow all objects to actually load in (?)

        if (DetailedLogging)
            Logger.LogInfo("Checking new scene for audio sources...");

        foreach (var audio in FindObjectsOfType<AudioSource>(true))
        {
            if (DetailedLogging)
                Logger.LogInfo($"  {audio.name} - {audio.clip?.name ?? "<No clip>"}");

            Reroute(audio);
        }
    }
}