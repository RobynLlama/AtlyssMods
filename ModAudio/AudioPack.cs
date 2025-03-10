using UnityEngine;
using static Marioalexsan.ModAudio.AudioPackConfig;

namespace Marioalexsan.ModAudio;

public class AudioPack
{
    public string PackPath { get; set; } = "???";
    public bool Enabled { get; set; } = false; // Must be enabled before use

    public AudioPackConfig Config { get; set; } = new();

    public Dictionary<string, Func<AudioClip>> DelayedLoadClips { get; } = [];
    public Dictionary<string, AudioClip> LoadedClips { get; } = [];
    public Dictionary<string, List<ClipReplacement>> Replacements { get; } = [];

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
