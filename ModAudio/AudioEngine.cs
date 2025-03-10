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
    }

    private static readonly System.Random RNG = new();
    private static readonly Dictionary<AudioSource, SourceState> TrackedSources = [];

    private static readonly Stack<AudioSource> SourceCache = new Stack<AudioSource>(256);
    private static readonly List<AudioSource> OneShotAudioSources = [];

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
        Logger.LogInfo("Reloading engine...");

        if (hardReload)
        {
            foreach (var source in OneShotAudioSources)
            {
                if (source != null)
                    UnityEngine.Object.Destroy(source);
            }

            OneShotAudioSources.Clear();
        }

        foreach (var source in TrackedSources)
        {
            // Restore previous state
            if (source.Value.Touched)
            {
                // AudioSource needs to be restarted for the new originalClip to be applied
                var wasPlaying = source.Key.isPlaying;
                source.Key.Stop();

                source.Key.clip = source.Value.Clip;
                source.Key.volume = source.Value.Volume;
                source.Key.pitch = source.Value.Pitch;
            }
        }

        TrackedSources.Clear();

        if (hardReload)
        {
            Logger.LogInfo("Reloading audio packs...");

            AudioPacks.Clear();
            AudioPacks.AddRange(AudioPackLoader.LoadAudioPacks());
        }

        foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
        {
            AudioPlayed(audio);
        }

        Logger.LogInfo("Done with reload!");
    }

    public static void Update()
    {
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

        foreach (var source in OneShotAudioSources)
        {
            if (source == null || !source.isPlaying)
                SourceCache.Push(source);
        }

        while (SourceCache.Count > 0)
        {
            var source = SourceCache.Pop();

            if (source != null)
                UnityEngine.Object.Destroy(source);

            OneShotAudioSources.Remove(source);
        }

        // Check for any changes in tracked sources


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

        oneShotSource.name = "modaudio_source";

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

        // TODO custom curves?
        // TODO other properties?

        return oneShotSource;
    }

    public static bool AudioPlayed(AudioSource source)
    {
        Logger.LogInfo($"Audio source playing: source {source.name}, clip {source.clip}", ModAudio.Plugin.LogPlayedAudio);

        if (OneShotAudioSources.Contains(source))
            return true; // Do not reprocess these, would probably create an infinite loop somewhere

        TrackSource(source);

        Reroute(source);
        ApplyEffects(source);
        ApplyTriggers(source);

        return true;
    }

    public static bool ClipPlayed(AudioClip clip, AudioSource source)
    {
        Logger.LogInfo($"Clip playing: clip {source.clip}, host source {source.name}", ModAudio.Plugin.LogPlayedAudio);

        if (TryGetReplacement(clip, out var destination) && clip != destination)
        {
            Logger.LogInfo($"Clip replaced: {clip.name} => {destination.name}", ModAudio.Plugin.LogCustomAudio);
            clip = destination;
        }

        // Move to a dedicated audio source for better control. Note: This is likely overkill and might mess with other mods?

        var oneShotSource = CreateOneShotFromSource(source);
        oneShotSource.clip = clip;

        OneShotAudioSources.Add(oneShotSource);

        TrackSource(oneShotSource);

        ApplyEffects(oneShotSource);
        ApplyTriggers(oneShotSource);

        oneShotSource.Play();

        return false;
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

                if (trigger.TargetAudioSources.Count > 0 && !trigger.TargetAudioSources.Contains(source.name))
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

                    OneShotAudioSources.Add(oneShotSource);

                    TrackSource(oneShotSource);

                    TrackedSources[oneShotSource] = TrackedSources[oneShotSource] with { PlayAudioEventsApplied = true };

                    ApplyEffects(oneShotSource);
                    // No ApplyTriggers(oneShotSource) here - play triggers shouldn't trigger other triggers

                    oneShotSource.Play();
                }
                else
                {
                    Logger.LogWarning($"TODO Couldn't get clip {selectedClipData} to play for audio event!");
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
            foreach (var effect in pack.Config.AudioEffects)
            {
                // TODO: Effects that don't specify any targets or modifiers should be considered as invalid

                if (effect.TargetClips.Count > 0 && !effect.TargetClips.Contains(source.clip?.name))
                    continue;

                if (effect.TargetAudioSources.Count > 0 && !effect.TargetAudioSources.Contains(source.name))
                    continue;

                // AudioSource needs to be restarted for the new pitch to be applied
                var wasPlaying = source.isPlaying;
                source.Stop();

                if (effect.VolumeModifier.HasValue)
                {
                    source.volume *= effect.VolumeModifier.Value;
                }

                if (effect.PitchModifier.HasValue)
                {
                    source.pitch *= effect.PitchModifier.Value;
                }

                if (wasPlaying)
                    source.Play();

                Logger.LogInfo($"TODO Applied effect to {source.name} {source.clip.name}: volume {effect.VolumeModifier.GetValueOrDefault(1)}, pitch {effect.PitchModifier.GetValueOrDefault(1)}", ModAudio.Plugin.LogAudioEffects);
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

            // AudioSource needs to be restarted for the new originalClip to be applied
            var wasPlaying = source.isPlaying;
            source.Stop();

            source.clip = destination;

            if (wasPlaying)
                source.Play();

            Logger.LogInfo($"Rerouted source {source.name} from {originalClip.name} to {destination.name}.", ModAudio.Plugin.LogCustomAudio);
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
