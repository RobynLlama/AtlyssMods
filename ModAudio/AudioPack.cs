using UnityEngine;

namespace Marioalexsan.ModAudio;

public class AudioPack
{
    public string PackPath { get; set; } = "???";
    public bool Enabled { get; set; } = false; // Must be enabled before use
    public bool OverrideModeEnabled { get; set; } = false;

    public AudioPackConfig Config { get; set; } = new();

    public List<AudioClipLoader.IAudioStream> OpenStreams { get; } = []; // Only touch this if you plan on cleaning up the pack

    public Dictionary<string, AudioClip> ReadyClips { get; } = [];

    // These clips are loaded / streamed when needed
    public Dictionary<string, Func<AudioClip>> PendingClipsToLoad { get; } = [];
    public Dictionary<string, Func<AudioClip>> PendingClipsToStream { get; } = [];

    public bool IsUserPack()
    {
        return PackPath.StartsWith(ModAudio.Plugin.ModAudioConfigFolder) ||
            PackPath.StartsWith(ModAudio.Plugin.ModAudioPluginFolder);
    }

    public bool TryGetReadyClip(string name, out AudioClip clip)
    {
        if (ReadyClips.TryGetValue(name, out clip))
            return true;

        if (PendingClipsToStream.TryGetValue(name, out var streamer))
        {
            PendingClipsToStream.Remove(name);
            clip = ReadyClips[name] = streamer();
            return true;
        }

        if (PendingClipsToLoad.TryGetValue(name, out var loader))
        {
            PendingClipsToLoad.Remove(name);
            clip = ReadyClips[name] = loader();
            return true;
        }

        clip = null;
        return false;
    }
}
