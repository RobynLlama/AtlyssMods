using UnityEngine;
using static Marioalexsan.ModAudio.AudioPackConfig;

namespace Marioalexsan.ModAudio;

public class AudioPack
{
    public string PackPath { get; set; } = "???";
    public bool Enabled { get; set; } = false; // Must be enabled before use
    public bool OverrideModeEnabled { get; set; } = false;

    public AudioPackConfig Config { get; set; } = new();

    public Dictionary<string, Func<AudioClip>> DelayedLoadClips { get; } = [];
    public Dictionary<string, AudioClip> LoadedClips { get; } = [];

    public bool IsUserPack()
    {
        return PackPath.StartsWith(ModAudio.Plugin.ModAudioConfigFolder) ||
            PackPath.StartsWith(ModAudio.Plugin.ModAudioPluginFolder);
    }

    public bool TryGetReadyClip(string name, out AudioClip clip)
    {
        if (LoadedClips.TryGetValue(name, out clip))
            return true;

        if (DelayedLoadClips.TryGetValue(name, out var loader))
        {
            clip = LoadedClips[name] = loader();
            return true;
        }

        clip = null;
        return false;
    }
}
