using System.Collections.Concurrent;
using UnityEngine;

namespace Marioalexsan.ModAudio;

public class AudioPack
{
    public string PackPath { get; set; } = "???";
    public bool Enabled { get; set; } = false; // Must be enabled before use
    public bool OverrideModeEnabled { get; set; } = false;

    public AudioPackConfig Config { get; set; } = new();

    public List<AudioClipLoader.IAudioStream> OpenStreams { get; } = []; // Only touch this if you plan on cleaning up the pack

    public ConcurrentDictionary<string, AudioClip> ReadyClips { get; } = [];

    // These clips are loaded / streamed when needed
    public ConcurrentDictionary<string, Func<AudioClip>> PendingClipsToLoad { get; } = [];
    public ConcurrentDictionary<string, Func<AudioClip>> PendingClipsToStream { get; } = [];

    public bool IsUserPack()
    {
        return PackPath.StartsWith(ModAudio.Plugin.ModAudioConfigFolder) ||
            PackPath.StartsWith(ModAudio.Plugin.ModAudioPluginFolder);
    }

    public Task<bool> TryGetReadyClip(string name, out AudioClip? clip)
    {
        if (ReadyClips.TryGetValue(name, out clip))
            return Task.FromResult(true);

        /*
            Note for the changes below: I suspect this is doing
            too many checks since ConcurrentDictionary.Remove also
            returns the value it removed but I don't know enough
            to modify these calls to cut down on cycles. Probably
            not a huge deal, anyway.
                Robyn
        */

        if (PendingClipsToStream.TryGetValue(name, out var streamer))
        {
            //Discard the removed value
            PendingClipsToStream.Remove(name, out var _);
            clip = ReadyClips[name] = streamer();
            return Task.FromResult(true);
        }

        if (PendingClipsToLoad.TryGetValue(name, out var loader))
        {
            //Discard the removed value
            PendingClipsToLoad.Remove(name, out var _);
            clip = ReadyClips[name] = loader();
            return Task.FromResult(true);
        }

        clip = null;
        return Task.FromResult(false);
    }
}
