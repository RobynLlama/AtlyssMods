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
    private ConditionalWeakTable<AudioSource, WeakReference<AudioClip>> _originalSources = new();

    private bool _ranAssetModsDetection;
    //private GameObject _debugDisplay;

    private List<string> _modFoldersChecked = [];

    private readonly Dictionary<string, Dictionary<string, AudioClip>> _customAudio = [];
    private readonly Dictionary<string, Dictionary<string, string>> _customRoutes = [];

    private string AudioLocation => Path.Combine(Path.GetDirectoryName(Info.Location), "audio");
    private string AudioReference => Path.Combine(AudioLocation, "__README.txt");
    private string AudioRoutes => Path.Combine(AudioLocation, "__routes.txt");

    public bool DetailedLogging { get; private set; }
    //public KeyCode DebugKeybind { get; private set; }
    //public bool DebugDisplayActive { get; private set; }

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
        //DebugKeybind = Config.Bind("General", "DebugKeybind", KeyCode.Keypad0, "Keybinding used to toggle the debug display for audio.").Value;
        //_debugDisplay = DebugDisplay.Create();

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
                var routes = Path.Combine(folder, "__routes.txt");

                if (_modFoldersChecked.Contains(Path.GetDirectoryName(routes)))
                    continue;  // Already added via API

                if (File.Exists(routes))
                    LoadAudioFromLocation(Path.GetDirectoryName(routes), "ModAudio:path//" + routes);

                var audio = Path.Combine(folder, "audio", "__routes.txt");

                if (_modFoldersChecked.Contains(Path.GetDirectoryName(audio)))
                    continue;  // Already added via API

                if (Directory.Exists(Path.GetDirectoryName(audio)))
                    LoadAudioFromLocation(Path.GetDirectoryName(audio), "ModAudio:path//" + audio);
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
        if (!_customAudio.TryGetValue(id, out var audio))
            audio = _customAudio[id] = [];

        if (!_customRoutes.TryGetValue(id, out var routes))
            routes = _customRoutes[id] = [];

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
            var data = RouteConfig.Read(routesPath);

            foreach (var kvp in data)
                routes.Add(kvp.Key, kvp.Value);
        }

        _modFoldersChecked.Add(Path.GetDirectoryName(routesPath));

        if (audio.Count == 0 && routes.Count == 0)
        {
            _customAudio.Remove(id);
            _customRoutes.Remove(id);
        }
        else
        {
            bool usedPath = id.StartsWith("ModAudio:path//");
            string idClean = id.Replace("ModAudio:path//", "");

            Logger.LogInfo($"Loaded audio from {(usedPath ? "path" : "mod")} {id} ({audio.Count} clips, {routes.Count} routes).");
        }
    }

    public void Reroute(AudioSource input)
    {
        if (Resolve(input.clip, out var destination))
        {
            if (input.name == destination.name)
                return; // No point, same clip

            if (DetailedLogging)
                Logger.LogInfo($"Rerouted \"{input.clip.name}\" => \"{destination.name}\"");

            if (!_originalSources.TryGetValue(input, out _))
                _originalSources.Add(input, new WeakReference<AudioClip>(input.clip));

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

        if (_customAudio.ContainsKey(ModInfo.PLUGIN_GUID))
        {
            if (_customAudio[ModInfo.PLUGIN_GUID].TryGetValue(source.name, out destination))
                return true;

            if (_customRoutes[ModInfo.PLUGIN_GUID].TryGetValue(source.name, out var routed) && _customAudio[ModInfo.PLUGIN_GUID].TryGetValue(routed, out destination))
                return true;
        }

        foreach (var guid in _customAudio.Keys)
        {
            if (guid == ModInfo.PLUGIN_GUID)
                continue;

            if (_customAudio[guid].TryGetValue(source.name, out destination))
                return true;

            if (_customRoutes[guid].TryGetValue(source.name, out var routed) && _customAudio[guid].TryGetValue(routed, out destination))
                return true;
        }

        destination = null;
        return false;
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