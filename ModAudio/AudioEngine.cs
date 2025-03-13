using Mono.Cecil;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Windows;
using static UnityEngine.Random;

namespace Marioalexsan.ModAudio;

internal static class AudioEngine
{
    private struct SourceState
    {
        // Previous state
        public AudioClip Clip;
        public float Volume;
        public float Pitch;

        // Last applied state
        public AudioClip AppliedClip;

        // Flags
        public readonly bool Touched => EffectsApplied || Rerouted;
        public bool EffectsApplied;
        public bool Rerouted;
        public bool PlayAudioEventsApplied;
        public bool IsOneShotSource;
        public bool ProcessedOnStart;

        // Misc
        public AudioSource OneShotOrigin;
    }

    private static readonly System.Random RNG = new();
    private static readonly Dictionary<AudioSource, SourceState> TrackedSources = [];
    private static readonly HashSet<AudioSource> TrackedPlayOnAwakeSources = [];

    private static readonly Stack<AudioSource> SourceCache = new Stack<AudioSource>(256);

    public static List<AudioPack> AudioPacks { get; } = [];
    public static IEnumerable<AudioPack> EnabledPacks => AudioPacks.Where(x => x.Enabled);

    private static bool IsUserPack(AudioPack pack)
    {
        return pack.PackPath.StartsWith(ModAudio.Plugin.ModAudioConfigFolder) ||
            pack.PackPath.StartsWith(ModAudio.Plugin.ModAudioPluginFolder);
    }

    public static void HardReload() => Reload(hardReload: true);

    public static void SoftReload() => Reload(hardReload: false);

    private static void Reload(bool hardReload)
    {
        Logging.LogInfo("Reloading engine...");

        if (hardReload)
        {
            // Get rid of one-shots forcefully
            SourceCache.Clear();

            foreach (var source in TrackedSources)
            {
                if (source.Value.IsOneShotSource)
                {
                    SourceCache.Push(source.Key);
                }
            }

            while (SourceCache.Count > 0)
            {
                var source = SourceCache.Pop();
                TrackedSources.Remove(source);
                UnityEngine.Object.Destroy(source);
            }
        }

        // Restore previous state
        foreach (var source in TrackedSources)
        {
            SourceCache.Push(source.Key);

            if (source.Value.Touched)
            {
                source.Key.clip = source.Value.Clip;
                source.Key.volume = source.Value.Volume;
                source.Key.pitch = source.Value.Pitch;
            }
        }

        TrackedSources.Clear();

        if (hardReload)
        {
            Logging.LogInfo("Reloading audio packs...");

            AudioPacks.Clear();
            AudioPacks.AddRange(AudioPackLoader.LoadAudioPacks());
        }

        // Restart audio
        foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
        {
            //AudioPlayed(audio);

            if (audio.isPlaying)
            {
                audio.Stop();
                audio.Play();
            }
        }

        Logging.LogInfo("Done with reload!");
    }

    public static void Update()
    {
        // Check play on awake sounds
        bool checkPlayOnAwake = true;

        if (checkPlayOnAwake)
        {
            foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
            {
                if (audio.playOnAwake)
                {
                    if (!TrackedPlayOnAwakeSources.Contains(audio) && audio.isActiveAndEnabled)
                    {
                        TrackedPlayOnAwakeSources.Add(audio);
                        AudioPlayed(audio);
                    }
                    else if (TrackedPlayOnAwakeSources.Contains(audio) && !audio.isActiveAndEnabled)
                    {
                        TrackedPlayOnAwakeSources.Remove(audio);
                    }
                }
            }
        }

        // Cleanup dead play on awake sounds
        SourceCache.Clear();

        foreach (var source in TrackedPlayOnAwakeSources)
        {
            if (source == null)
                SourceCache.Push(source);
        }

        while (SourceCache.Count > 0)
        {
            TrackedPlayOnAwakeSources.Remove(SourceCache.Pop());
        }

        // Cleanup stale stuff
        SourceCache.Clear();

        foreach (var source in TrackedSources)
        {
            if (source.Key == null)
                SourceCache.Push(source.Key);
        }

        while (SourceCache.Count > 0)
        {
            TrackedSources.Remove(SourceCache.Pop());
        }

        // Cleanup dead one shot sources
        SourceCache.Clear();

        foreach (var source in TrackedSources)
        {
            if (source.Value.IsOneShotSource && !source.Key.isPlaying)
                SourceCache.Push(source.Key);
        }

        while (SourceCache.Count > 0)
        {
            var source = SourceCache.Pop();
            TrackedSources.Remove(source);

            if (source != null)
                UnityEngine.Object.Destroy(source);
        }

        // Check for any changes in tracked sources' clips
        foreach (var source in TrackedSources)
        {
            if (source.Key.clip != source.Value.AppliedClip)
                SourceCache.Push(source.Key);
        }

        while (SourceCache.Count > 0)
        {
            var source = SourceCache.Pop();
            TrackedSources[source] = TrackedSources[source] with { Clip = source.clip, Rerouted = false };
        }
    }

    private static void TrackSource(AudioSource source)
    {
        if (!TrackedSources.ContainsKey(source))
        {
            TrackedSources.Add(source, new()
            {
                AppliedClip = source.clip,
                Clip = source.clip,
                Pitch = source.pitch,
                Volume = source.volume
            });
        }
    }

    private static AudioSource CreateOneShotFromSource(AudioSource source)
    {
        var oneShotSource = source.gameObject.AddComponent<AudioSource>();

        oneShotSource.name = "modaudio_oneshot";

        oneShotSource.volume = source.volume;
        oneShotSource.pitch = source.pitch;
        oneShotSource.clip = source.clip;
        oneShotSource.outputAudioMixerGroup = source.outputAudioMixerGroup;
        oneShotSource.loop = false; // Otherwise this won't play one-shot
        oneShotSource.ignoreListenerVolume = source.ignoreListenerVolume;
        oneShotSource.ignoreListenerPause = source.ignoreListenerPause;
        oneShotSource.velocityUpdateMode = source.velocityUpdateMode;
        oneShotSource.panStereo = source.panStereo;
        oneShotSource.spatialBlend = source.spatialBlend;
        oneShotSource.spatialize = source.spatialize;
        oneShotSource.spatializePostEffects = source.spatializePostEffects;
        oneShotSource.reverbZoneMix = source.reverbZoneMix;
        oneShotSource.bypassEffects = source.bypassEffects;
        oneShotSource.bypassListenerEffects = source.bypassListenerEffects;
        oneShotSource.bypassReverbZones = source.bypassReverbZones;
        oneShotSource.dopplerLevel = source.dopplerLevel;
        oneShotSource.spread = source.spread;
        oneShotSource.priority = source.priority;
        oneShotSource.mute = source.mute;
        oneShotSource.minDistance = source.minDistance;
        oneShotSource.maxDistance = source.maxDistance;
        oneShotSource.rolloffMode = source.rolloffMode;

        oneShotSource.playOnAwake = false; // This should be false for one shot sources, but whatever

        oneShotSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, source.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        oneShotSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, source.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
        oneShotSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, source.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
        oneShotSource.SetCustomCurve(AudioSourceCurveType.Spread, source.GetCustomCurve(AudioSourceCurveType.Spread));

        TrackSource(oneShotSource);
        TrackedSources[oneShotSource] = TrackedSources[oneShotSource] with { IsOneShotSource = true, OneShotOrigin = source };

        return oneShotSource;
    }

    public static bool AudioStopped(AudioSource source, bool stopOneShots)
    {
        if (stopOneShots)
        {
            foreach (var trackedSource in TrackedSources)
            {
                if (trackedSource.Value.IsOneShotSource && trackedSource.Value.OneShotOrigin == source && trackedSource.Key != null && trackedSource.Key.isPlaying)
                    trackedSource.Key.Stop();
            }
        }

        return true;
    }

    public static bool OneShotClipPlayed(AudioClip clip, AudioSource source, float volumeScale)
    {
        // Move to a dedicated audio source for better control. Note: This is likely overkill and might mess with other mods?

        var oneShotSource = CreateOneShotFromSource(source);
        oneShotSource.volume *= volumeScale;
        oneShotSource.clip = clip;

        TrackedSources[oneShotSource] = TrackedSources[oneShotSource] with { Clip = clip, Volume = oneShotSource.volume };

        oneShotSource.Play();

        return false;
    }

    public static bool AudioPlayed(AudioSource source)
    {
        bool processed = TrackedSources.TryGetValue(source, out var state) && state.ProcessedOnStart;
        bool restarted = false;

        if (!processed)
        {
            TrackSource(source);
            TrackedSources[source] = TrackedSources[source] with { ProcessedOnStart = true };

            Reroute(source);
            ApplyEffects(source);
            ApplyTriggers(source);

            if (source.isPlaying)
            {
                restarted = true;
                source.Stop();
                source.Play();
            }
        }

        // If it has been restarted, then Play() has been called again
        // This means that we need to skip logging and running the original method for this call
        if (!restarted)
        {
            var originalClipName = TrackedSources[source].Clip?.name ?? "(null)";
            var currentClipName = source.clip?.name ?? "(null)";
            bool rerouted = TrackedSources[source].Rerouted;

            Logging.LogInfo($"Clip {originalClipName} ({source.name}){(rerouted ? $" => {currentClipName}" : "")}", ModAudio.Plugin.LogPlayedAudio);
        }

        return !restarted;
    }

    private static void ApplyTriggers(AudioSource source)
    {
        if (TrackedSources.TryGetValue(source, out var state) && state.PlayAudioEventsApplied)
            return;

        TrackedSources[source] = TrackedSources[source] with { PlayAudioEventsApplied = true };

        foreach (var pack in EnabledPacks)
        {
            foreach (var trigger in pack.Config.PlayAudioEvents)
            {
                // TODO: Triggers that don't specify any targets or clips should be considered as invalid

                if (trigger.ClipSelection.Count == 0)
                    continue;

                if (trigger.TargetClips.Count > 0 && !trigger.TargetClips.Contains(source.clip?.name))
                    continue;

                if (trigger.TargetSources.Count > 0 && !trigger.TargetSources.Contains(source.name))
                    continue;

                if (RNG.NextDouble() > trigger.TriggerChance)
                    continue;

                float totalWeight = 0f;

                foreach (var clip in trigger.ClipSelection)
                {
                    totalWeight += clip.RandomWeight;
                }

                float randomValue = (float)(RNG.NextDouble() * totalWeight);
                var selectedClipData = trigger.ClipSelection[0]; // Use source as default if rolling doesn't succeed for some reason

                foreach (var clip in trigger.ClipSelection)
                {
                    if (randomValue <= 0)
                        break;

                    randomValue -= clip.RandomWeight;
                    selectedClipData = clip;
                }

                if (pack.TryGetReadyClip(selectedClipData.ClipName, out var selectedClip))
                {
                    var oneShotSource = CreateOneShotFromSource(source);

                    oneShotSource.pitch = selectedClipData.Pitch;
                    oneShotSource.volume = selectedClipData.Volume;
                    oneShotSource.clip = selectedClip;

                    TrackedSources[oneShotSource] = TrackedSources[oneShotSource] with { 
                        PlayAudioEventsApplied = true,
                        Pitch = oneShotSource.pitch,
                        Volume = oneShotSource.volume,
                        Clip = oneShotSource.clip
                    };

                    oneShotSource.Play();

                    Logging.LogInfo($"Triggered clip {oneShotSource.clip?.name ?? "(null)"} on {source.clip?.name} ({source.name}).", ModAudio.Plugin.LogAudioEffects);
                }
                else
                {
                    Logging.LogWarning($"TODO Couldn't get clip {selectedClipData} to play for audio event!");
                }
            }
        }
    }

    private static void ApplyEffects(AudioSource source)
    {
        if (TrackedSources.TryGetValue(source, out var state) && state.EffectsApplied)
            return;

        TrackedSources[source] = TrackedSources[source] with { EffectsApplied = true };

        bool effectApplied = false; // For now this means there's at most one effect per clip

        foreach (var pack in EnabledPacks)
        {
            foreach (var effect in pack.Config.Effects)
            {
                // TODO: Effects that don't specify any targets or modifiers should be considered as invalid

                if (effect.TargetClips.Count > 0 && !effect.TargetClips.Contains(source.clip?.name))
                    continue;

                if (effect.TargetSources.Count > 0 && !effect.TargetSources.Contains(source.name))
                    continue;

                if (effect.Volume.HasValue)
                {
                    source.volume *= effect.Volume.Value;
                }

                if (effect.Pitch.HasValue)
                {
                    source.pitch *= effect.Pitch.Value;
                }

                Logging.LogInfo($"Applied effects to clip {source.clip?.name ?? "(null)"}: volume {effect.Volume.GetValueOrDefault(1)}, pitch {effect.Pitch.GetValueOrDefault(1)}", ModAudio.Plugin.LogAudioEffects);
                effectApplied = true;
                break;
            }

            if (effectApplied)
                break;
        }

        return;
    }

    private static void Reroute(AudioSource source)
    {
        if (TrackedSources.TryGetValue(source, out var state) && state.Rerouted)
            return;

        var originalClip = TrackedSources[source].Clip;

        if (TryGetReplacement(originalClip, out var destination))
        {
            if (source.clip.name == destination.name)
                return; // Same clip in audio source at the moment, don't need to do operations on it?

            TrackedSources[source] = TrackedSources[source] with { AppliedClip = destination, Rerouted = true };

            source.clip = destination;

            Logging.LogInfo($"Replaced clip {originalClip.name} with {destination.name}.", ModAudio.Plugin.LogCustomAudio);
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

        foreach (var pack in EnabledPacks.Where(IsUserPack))
        {
            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    totalWeight += branch.RandomWeight;
                }
            }
        }

        foreach (var pack in EnabledPacks.Where(x => !IsUserPack(x)))
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

        foreach (var pack in EnabledPacks.Where(IsUserPack))
        {
            if (randomValue <= 0)
                break;

            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    if (branch.Target == ModAudio.DefaultClipIdentifier || !pack.TryGetReadyClip(branch.Target, out selectedClip))
                    {
                        selectedClip = source;
                    }

                    randomValue -= branch.RandomWeight;

                    if (randomValue <= 0)
                        break;
                }
            }
        }

        foreach (var pack in EnabledPacks.Where(x => !IsUserPack(x)))
        {
            if (randomValue <= 0)
                break;

            if (pack.Replacements.TryGetValue(source.name, out var replacementData))
            {
                foreach (var branch in replacementData)
                {
                    if (branch.Target == ModAudio.DefaultClipIdentifier || !pack.TryGetReadyClip(branch.Target, out selectedClip))
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
