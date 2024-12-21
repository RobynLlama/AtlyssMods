using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.ModAudio;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class ModAudio : BaseUnityPlugin
{
    public static ModAudio Instance { get; private set; }
    internal new ManualLogSource Logger { get; private set; }

    private Harmony _harmony;
    private ConditionalWeakTable<AudioSource, WeakReference<AudioClip>> _originalSources = new();

    private readonly Dictionary<string, Dictionary<string, AudioClip>> _customAudio = [];
    private readonly Dictionary<string, Dictionary<string, string>> _customRoutes = [];

    private string AudioLocation => Path.Combine(Path.GetDirectoryName(Info.Location), "audio");
    private string AudioReference => Path.Combine(AudioLocation, "__README.txt");
    private string AudioRoutes => Path.Combine(AudioLocation, "__routes.txt");

    public void LoadModAudio(BaseUnityPlugin plugin, string audioFolder = null)
    {
        if (plugin == this)
            return;

        LoadAudio(plugin, audioFolder);
    }

    private void LoadAudio(BaseUnityPlugin plugin, string audioFolder)
    {
        if (!_customAudio.TryGetValue(plugin.Info.Metadata.GUID, out var audio))
            audio = _customAudio[plugin.Info.Metadata.GUID] = [];

        if (!_customRoutes.TryGetValue(plugin.Info.Metadata.GUID, out var routes))
            routes = _customRoutes[plugin.Info.Metadata.GUID] = [];

        audioFolder ??= Path.Combine(Path.GetDirectoryName(plugin.Info.Location), "audio");

        foreach (var file in Directory.GetFiles(audioFolder))
        {
            var name = Path.GetFileNameWithoutExtension(file);

            if (audio.ContainsKey(name))
                return;

            var clip = ClipLoader.LoadFromFile(name, file);

            if (clip != null)
                audio[name] = clip;
        }

        var routesPath = Path.Combine(audioFolder, "__routes.txt");

        if (File.Exists(routesPath))
        {
            var data = RouteConfig.Read(routesPath);

            foreach (var kvp in data)
                routes.Add(kvp.Key, kvp.Value);
        }

        Logger.LogInfo($"Loaded audio from mod {plugin.Info.Metadata.GUID} ({audio.Count} clips, {routes.Count} routes).");
    }

    public void Reroute(AudioSource input)
    {
        if (Resolve(input.clip, out var destination))
        {
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

    private bool Resolve(AudioClip source, out AudioClip destination)
    {
        if (source?.name == null)
        {
            destination = null;
            return false;
        }

        if (_customAudio[ModInfo.PLUGIN_GUID].TryGetValue(source.name, out destination))
            return true;

        if (_customRoutes[ModInfo.PLUGIN_GUID].TryGetValue(source.name, out var routed) && _customAudio[ModInfo.PLUGIN_GUID].TryGetValue(routed, out destination))
            return true;

        foreach (var guid in _customAudio.Keys)
        {
            if (guid == ModInfo.PLUGIN_GUID)
                continue;

            if (_customAudio[guid].TryGetValue(source.name, out destination))
                return true;

            if (_customRoutes[guid].TryGetValue(source.name, out routed) && _customAudio[guid].TryGetValue(routed, out destination))
                return true;
        }

        return false;
    }

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
            File.Create(AudioRoutes);

        VanillaClipNames.GenerateReferenceFile(AudioReference);

        Logger.LogInfo("Loading custom audio...");
        LoadAudio(this, AudioLocation);

        Logger.LogInfo("Initialized successfully!");
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        StartCoroutine(CheckNewScene());
    }

    System.Collections.IEnumerator CheckNewScene()
    {
        yield return null; // A frame delay should allow all objects to actually load in

        foreach (var audio in FindObjectsOfType<AudioSource>())
            Reroute(audio);
    }
}