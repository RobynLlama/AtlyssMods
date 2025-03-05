using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;

namespace Marioalexsan.ModAudio;

internal static class AudioEngine
{
    private static ManualLogSource Logger => ModAudio.Plugin.Logger;

    private struct OriginalAudioState
    {
        public AudioClip OriginalClip;
    }

    private static readonly System.Random RNG = new();
    private static readonly Dictionary<AudioSource, OriginalAudioState> OriginalSources = [];

    private static readonly Stack<AudioSource> SourceCache = new Stack<AudioSource>(256);

    public static List<AudioPack> AudioPacks { get; } = [];

    private static bool IsUserPack(AudioPack pack)
    {
        return pack.PackPath.StartsWith(ModAudio.Plugin.ModAudioConfigFolder) ||
            pack.PackPath.StartsWith(ModAudio.Plugin.ModAudioPluginFolder);
    }

    public static bool Freezed { get; set; } = false;

    public static void Reload()
    {
        SoftReload();
        AudioPacks.Clear();
        AudioPacks.AddRange(AudioPackLoader.LoadAudioPacks());
    }

    public static void AudioPlayed(AudioSource source)
    {
        Logger.LogInfo($"Audio played {source.name}: {source.clip?.name} ");
        Reroute(source);
    }

    public static void ClipPlayed(ref AudioClip clip, AudioSource source)
    {
        Logger.LogInfo($"Clip played at source {source?.name}: {clip.name} ");
        if (TryGetReplacement(clip, out var destination))
        {
            clip = destination;
        }
    }

    public static void Update()
    {
        // Cleanup stale stuff
        SourceCache.Clear();

        foreach (var source in OriginalSources)
        {
            if (source.Key == null)
                SourceCache.Push(source.Key);
        }

        while (SourceCache.Count > 0)
        {
            OriginalSources.Remove(SourceCache.Pop());
        }
    }

    public static void SoftReload()
    {
        foreach (var source in OriginalSources)
        {
            SourceCache.Push(source.Key);

            Logger.LogInfo($"Restoring state for {source.Key.name} from {source.Key.clip?.name} to {source.Value.OriginalClip?.name}");

            // AudioSource needs to be restarted for the new clip to be applied
            var wasPlaying = source.Key.isPlaying;
            source.Key.Stop();

            source.Key.clip = source.Value.OriginalClip;

            if (wasPlaying)
                source.Key.Play();
        }

        OriginalSources.Clear();

        foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
        {
            AudioPlayed(audio);
        }
    }

    private static void Reroute(AudioSource input)
    {
        var originalClip = input.clip;

        if (OriginalSources.TryGetValue(input, out var originalState))
        {
            originalClip = originalState.OriginalClip;
        }

        if (TryGetReplacement(originalClip, out var destination))
        {
            if (input.clip.name == destination.name)
                return; // Same clip in audio source at the moment, don't need to do operations on it

            if (!OriginalSources.TryGetValue(input, out _))
            {
                // Save the original clip if this is the first time we route it
                OriginalSources.Add(input, new()
                {
                    OriginalClip = originalClip
                });
            }

            Logger.LogInfo($"Rerouted {input.name}: {input.clip?.name} => {destination?.name}");

            // AudioSource needs to be restarted for the new clip to be applied
            var wasPlaying = input.isPlaying;
            input.Stop();

            input.clip = destination;

            if (wasPlaying)
                input.Play();
        }
    }

    private static bool TryGetReplacement(AudioClip source, out AudioClip destination)
    {
        if (source?.name == null)
        {
            destination = null;
            return false;
        }

        float totalWeight = 0;

        foreach (var pack in AudioPacks.Where(x => x.Enabled && IsUserPack(x)))
        {
            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    totalWeight += branch.RandomWeight;
                }
            }
        }

        foreach (var pack in AudioPacks.Where(x => x.Enabled && !IsUserPack(x)))
        {
            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    totalWeight += branch.RandomWeight;
                }
            }
        }

        float randomValue = (float)(RNG.NextDouble() * totalWeight);
        AudioClip selectedClip = source; // Use source as default if rolling doesn't succeed for some reason

        foreach (var pack in AudioPacks.Where(x => x.Enabled && IsUserPack(x)))
        {
            if (randomValue <= 0)
                break;

            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    if (branch.Target == ModAudio.DefaultClipIdentifier || !pack.LoadedClips.TryGetValue(branch.Target, out selectedClip))
                    {
                        selectedClip = source;
                    }

                    randomValue -= branch.RandomWeight;

                    if (randomValue <= 0)
                        break;
                }
            }
        }

        foreach (var pack in AudioPacks.Where(x => x.Enabled && !IsUserPack(x)))
        {
            if (randomValue <= 0)
                break;

            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    if (branch.Target == ModAudio.DefaultClipIdentifier || !pack.LoadedClips.TryGetValue(branch.Target, out selectedClip))
                    {
                        selectedClip = source;
                    }

                    randomValue -= branch.RandomWeight;

                    if (randomValue <= 0)
                        break;
                }
            }
        }

        destination = selectedClip;
        return true;
    }
}
