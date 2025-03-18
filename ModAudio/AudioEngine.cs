using Marioalexsan.ModAudio.HarmonyPatches;
using Mono.Cecil;
using UnityEngine;

namespace Marioalexsan.ModAudio;

internal static class AudioEngine
{
    private struct SourceState
    {
        // Previous state
        public AudioClip OriginalClip;
        public float Volume;
        public float Pitch;

        // (supposedly) Current state
        public AudioClip AppliedClip;

        // Flags
        public bool DisableRouting;
        public bool IsOneShotSource;
        public bool IsOverlay;

        // Temporary Flags
        public bool JustRouted;
        public bool JustUsedDefaultClip;

        // Misc
        public AudioSource OneShotOrigin;
    }

    private static readonly System.Random RNG = new();
    private static readonly Dictionary<AudioSource, SourceState> TrackedSources = [];
    private static readonly HashSet<AudioSource> TrackedPlayOnAwakeSources = [];

    private static readonly Stack<AudioSource> SourceCache = new Stack<AudioSource>(256);

    public static List<AudioPack> AudioPacks { get; } = [];
    public static IEnumerable<AudioPack> EnabledPacks => AudioPacks.Where(x => x.Enabled);

    private static AudioClip EmptyClip
    {
        get
        {
            if (_emptyClip == null)
            {
                _emptyClip = AudioClip.Create("___nothing___", 256, 1, 44100, false);
                _emptyClip.SetData(new float[256], 0);
            }

            return _emptyClip;
        }
    }
    private static AudioClip _emptyClip;

    private static bool IsValidTarget(AudioSource source, AudioPackConfig.Route route)
    {
        var trackedData = TrackedSources[source];

        var originalClipName = trackedData.OriginalClip?.name;
        bool matchesOriginalClip = false;

        if (originalClipName != null)
        {
            for (int i = 0; i < route.OriginalClips.Count; i++)
            {
                if (route.OriginalClips[i] == originalClipName)
                {
                    matchesOriginalClip = true;
                    break;
                }
            }
        }

        if (!matchesOriginalClip)
            return false;

        if (route.FilterBySources.Count > 0)
        {
            var sourceName = source.name;
            var matchesSource = false;

            for (int i = 0; i < route.FilterBySources.Count; i++)
            {
                if (route.FilterBySources[i] == sourceName)
                {
                    matchesSource = true;
                    break;
                }
            }

            if (!matchesSource)
                return false;
        }

        if (route.FilterByObject.Count > 0)
        {
            var transform = source.transform;

            var matchesObject = false;

            while (transform != null)
            {
                var gameObjectName = transform.gameObject.name;

                for (int i = 0; i < route.FilterByObject.Count; i++)
                {
                    if (route.FilterByObject[i] == gameObjectName)
                    {
                        matchesObject = true;
                        break;
                    }
                }

                transform = transform.parent;
            }

            if (!matchesObject)
                return false;
        }

        return true;
    }

    public static void HardReload() => Reload(hardReload: true);

    public static void SoftReload() => Reload(hardReload: false);

    private static void Reload(bool hardReload)
    {
        try
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
            Dictionary<AudioSource, bool> wasPlayingPreviously = [];

            foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
            {
                wasPlayingPreviously[audio] = audio.isPlaying;

                if (wasPlayingPreviously[audio])
                {
                    audio.Stop();
                }
            }

            foreach (var source in TrackedSources)
            {
                SourceCache.Push(source.Key);

                source.Key.clip = source.Value.OriginalClip;
                source.Key.volume = source.Value.Volume;
                source.Key.pitch = source.Value.Pitch;
            }

            TrackedSources.Clear();

            if (hardReload)
            {
                Logging.LogInfo("Reloading audio packs...");

                // Clean up handles from streams
                foreach (var pack in AudioPacks)
                {
                    foreach (var handle in pack.OpenStreams)
                        handle.Dispose();
                }

                AudioPacks.Clear();
                AudioPacks.AddRange(AudioPackLoader.LoadAudioPacks());
                ModAudio.Plugin.InitializePackConfiguration(); // TODO I wish ModAudio plugin ref wouldn't be here
            }

            // Restart audio
            foreach (var audio in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
            {
                if (wasPlayingPreviously[audio])
                    audio.Play();
            }

            Logging.LogInfo("Done with reload!");
        }
        catch (Exception e)
        {
            Logging.LogError($"ModAudio crashed in {nameof(Reload)}! Please report this error to the mod developer:");
            Logging.LogError(e.ToString());
            return;
        }
    }

    public static void Update()
    {
        try
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
        }
        catch (Exception e)
        {
            Logging.LogError($"ModAudio crashed in {nameof(Update)}! Please report this error to the mod developer:");
            Logging.LogError(e.ToString());
        }
    }

    private static void TrackSource(AudioSource source)
    {
        if (!TrackedSources.ContainsKey(source))
        {
            TrackedSources.Add(source, new()
            {
                AppliedClip = source.clip,
                OriginalClip = source.clip,
                Pitch = source.pitch,
                Volume = source.volume
            });
        }
    }

    private static AudioSource CreateOneShotFromSource(AudioSource source)
    {
        var oneShotSource = source.gameObject.AddComponent<AudioSource>();

        oneShotSource.name = "oneshot";

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
        try
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
        catch (Exception e)
        {
            Logging.LogError($"ModAudio crashed in {nameof(AudioStopped)}! Please report this error to the mod developer:");
            Logging.LogError(e.ToString());
            Logging.LogError($"AudioSource that caused the crash:");
            Logging.LogError($"  name = {source?.name ?? "(null)"}");
            Logging.LogError($"  clip = {source?.clip?.name ?? "(null)"}");
            Logging.LogError($"Parameter {nameof(stopOneShots)} was: {stopOneShots}");
            return true;
        }
    }

    public static bool OneShotClipPlayed(AudioClip clip, AudioSource source, float volumeScale)
    {
        try
        {
            // Move to a dedicated audio source for better control. Note: This is likely overkill and might mess with other mods?

            var oneShotSource = CreateOneShotFromSource(source);
            oneShotSource.volume *= volumeScale;
            oneShotSource.clip = clip;

            TrackedSources[oneShotSource] = TrackedSources[oneShotSource] with { OriginalClip = clip, Volume = oneShotSource.volume, AppliedClip = oneShotSource.clip };

            oneShotSource.Play();

            return false;
        }
        catch (Exception e)
        {
            Logging.LogError($"ModAudio crashed in {nameof(OneShotClipPlayed)}! Please report this error to the mod developer:");
            Logging.LogError(e.ToString());
            Logging.LogError($"AudioSource that caused the crash:");
            Logging.LogError($"  name = {source?.name ?? "(null)"}");
            Logging.LogError($"  clip = {source?.clip?.name ?? "(null)"}");
            Logging.LogError($"AudioClip that caused the crash:");
            Logging.LogError($"  name = {clip?.name ?? "(null)"}");
            Logging.LogError($"Parameter {nameof(volumeScale)} was: {volumeScale}");
            return true;
        }
    }

    private static void LogAudio(AudioSource source)
    {
        float distance = float.MinValue;

        if (ModAudio.Plugin.UseMaxDistanceForLogging.Value && (bool)Player._mainPlayer)
        {
            distance = Vector3.Distance(Player._mainPlayer.transform.position, source.transform.position);

            if (distance > ModAudio.Plugin.MaxDistanceForLogging.Value)
                return;
        }

        var groupName = source.outputAudioMixerGroup?.name?.ToLower() ?? "(null)"; // This can be null, apparently...

        if (!ModAudio.Plugin.LogAmbience.Value && groupName == "ambience")
            return;

        if (!ModAudio.Plugin.LogGame.Value && groupName == "game")
            return;

        if (!ModAudio.Plugin.LogGUI.Value && groupName == "gui")
            return;

        if (!ModAudio.Plugin.LogMusic.Value && groupName == "music")
            return;

        if (!ModAudio.Plugin.LogVoice.Value && groupName == "voice")
            return;

        var originalClipName = TrackedSources[source].OriginalClip?.name ?? "(null)";
        var currentClipName = source.clip?.name ?? "(null)";
        var clipChanged = TrackedSources[source].OriginalClip != source.clip;

        if (TrackedSources[source].JustUsedDefaultClip)
        {
            clipChanged = true;
            currentClipName = "___default___";
        }

        var originalVolume = TrackedSources[source].Volume;
        var currentVolume = source.volume;

        var originalPitch = TrackedSources[source].Pitch;
        var currentPitch = source.pitch;

        var clipDisplay = clipChanged ? $"{originalClipName} > {currentClipName}" : originalClipName;
        var volumeDisplay = originalVolume != currentVolume ? $"{originalVolume:F2} > {currentVolume:F2}" : $"{originalVolume:F2}";
        var pitchDisplay = originalPitch != currentPitch ? $"{originalPitch:F2} > {currentPitch:F2}" : $"{originalPitch:F2}";

        var messageDisplay = $"Clip {clipDisplay} | Src {source.name} | Vol {volumeDisplay} | Pit {pitchDisplay} | Grp {groupName}";

        if (distance != float.MinValue)
            messageDisplay += $" | Dst {distance:F2}";

        if (TrackedSources[source].IsOverlay)
            messageDisplay += " (overlay)";

        Logging.LogInfo(messageDisplay, ModAudio.Plugin.LogAudioPlayed);
    }

    public static bool AudioPlayed(AudioSource source)
    {
        try
        {
            var wasPlaying = source.isPlaying;

            if (!Route(source))
            {
                LogAudio(source);
                return true;
            }

            TrackedSources[source] = TrackedSources[source] with { JustRouted = false };

            bool requiresRestart = wasPlaying && !source.isPlaying;

            if (requiresRestart)
            {
                source.Play();
            }
            else
            {
                LogAudio(source);
            }

            // If a restart was required, then we already played the sound manually again, so let's skip the original
            return !requiresRestart;
        }
        catch (Exception e)
        {
            Logging.LogError($"ModAudio crashed in {nameof(AudioPlayed)}! Please report this error to the mod developer:");
            Logging.LogError(e.ToString());
            Logging.LogError($"AudioSource that caused the crash:");
            Logging.LogError($"  name = {source?.name ?? "(null)"}");
            Logging.LogError($"  clip = {source?.clip?.name ?? "(null)"}");
            return true;
        }
    }

    private static bool Route(AudioSource source)
    {
        TrackSource(source);
        var trackedData = TrackedSources[source];

        // Check for any changes in tracked sources' clips
        // If so, restore last volume / pitch and track new clip before routing

        if (source.clip != trackedData.AppliedClip)
        {
            // Restore previous modifiers and track new clip
            source.volume = trackedData.Volume;
            source.pitch = trackedData.Pitch;

            TrackedSources[source] = TrackedSources[source] with { OriginalClip = source.clip, AppliedClip = source.clip };
        }

        if (trackedData.JustRouted || trackedData.DisableRouting)
            return false;

        TrackedSources[source] = TrackedSources[source] with { JustRouted = true, JustUsedDefaultClip = false };

        // Get a replacement from routes

        List<(AudioPack, AudioPackConfig.Route)> replacements = [];

        foreach (var pack in EnabledPacks)
        {
            foreach (var route in pack.Config.Routes)
            {
                if (IsValidTarget(source, route))
                    replacements.Add((pack, route));
            }
        }

        (AudioPack Pack, AudioPackConfig.Route Route) replacementRoute = (null, null);

        if (replacements.Count > 0)
        {
            replacementRoute = SelectRandomWeighted(replacements);

            // Apply overall effects

            if (replacementRoute.Route.RelativeReplacementEffects)
            {
                source.volume = trackedData.Volume * replacementRoute.Route.Volume;
                source.pitch = trackedData.Pitch * replacementRoute.Route.Pitch;
            }
            else
            {
                source.volume = replacementRoute.Route.Volume;
                source.pitch = replacementRoute.Route.Pitch;
            }

            // Apply replacement if needed

            if (replacementRoute.Route.ReplacementClips.Count > 0)
            {
                var randomSelection = SelectRandomWeighted(replacementRoute.Route.ReplacementClips);

                AudioClip destinationClip;

                if (randomSelection.Name == "___default___")
                {
                    destinationClip = TrackedSources[source].OriginalClip;
                    TrackedSources[source] = TrackedSources[source] with { JustUsedDefaultClip = true };
                }
                else if (randomSelection.Name == "___nothing___")
                {
                    destinationClip = EmptyClip;
                }
                else
                {
                    replacementRoute.Pack.TryGetReadyClip(randomSelection.Name, out destinationClip);
                }

                if (destinationClip != null)
                {
                    TrackedSources[source] = TrackedSources[source] with { AppliedClip = destinationClip, JustRouted = true };

                    source.volume *= randomSelection.Volume;
                    source.pitch *= randomSelection.Pitch;

                    if (source.isPlaying)
                        source.Stop();

                    source.clip = destinationClip;
                }
                else
                {
                    Logging.LogWarning(Texts.AudioClipNotFound(randomSelection.Name));
                }
            }
        }

        List<(AudioPack Pack, AudioPackConfig.Route Route)> overlays = [];

        foreach (var pack in EnabledPacks)
        {
            foreach (var route in pack.Config.Routes)
            {
                if (route.OverlayClips.Count > 0 && IsValidTarget(source, route) && (!route.LinkOverlayAndReplacement || replacementRoute.Route == route)) 
                    overlays.Add((pack, route));
            }
        }

        if (overlays.Count > 0)
        {
            foreach (var (Pack, Route) in overlays)
            {
                var randomSelection = SelectRandomWeighted(Route.OverlayClips);

                if (randomSelection.Name == "___nothing___")
                    continue;

                if (Pack.TryGetReadyClip(randomSelection.Name, out var selectedClip))
                {
                    var oneShotSource = CreateOneShotFromSource(source);
                    oneShotSource.clip = selectedClip;

                    if (Route.RelativeOverlayEffects)
                    {
                        oneShotSource.volume = trackedData.Volume * randomSelection.Volume;
                        oneShotSource.pitch = trackedData.Pitch * randomSelection.Pitch;
                    }
                    else
                    {
                        oneShotSource.volume = randomSelection.Volume;
                        oneShotSource.pitch = randomSelection.Pitch;
                    }

                    TrackedSources[oneShotSource] = TrackedSources[oneShotSource] with
                    {
                        Pitch = oneShotSource.pitch,
                        Volume = oneShotSource.volume,
                        OriginalClip = oneShotSource.clip,
                        AppliedClip = oneShotSource.clip,
                        IsOverlay = true,
                        DisableRouting = true
                    };

                    oneShotSource.Play();
                }
                else
                {
                    Logging.LogWarning(Texts.AudioClipNotFound(randomSelection.Name));
                }
            }
        }

        return true;
    }

    private static (AudioPack Pack, AudioPackConfig.Route Route) SelectRandomWeighted(List<(AudioPack Pack, AudioPackConfig.Route Route)> routes)
    {
        var totalWeight = 0.0;

        for (int i = 0; i < routes.Count; i++)
            totalWeight += routes[i].Route.ReplacementWeight;
        
        var selectedIndex = -1;

        var randomValue = RNG.NextDouble() * totalWeight;

        do
        {
            selectedIndex++;
            randomValue -= routes[selectedIndex].Route.ReplacementWeight;
        }
        while (randomValue >= 0.0);

        return routes[selectedIndex];
    }

    private static AudioPackConfig.Route.ClipSelection SelectRandomWeighted(List<AudioPackConfig.Route.ClipSelection> selections)
    {
        var totalWeight = 0.0;

        for (int i = 0; i < selections.Count; i++)
            totalWeight += selections[i].Weight;

        var selectedIndex = -1;

        var randomValue = RNG.NextDouble() * totalWeight;

        do
        {
            selectedIndex++;
            randomValue -= selections[selectedIndex].Weight;
        }
        while (randomValue >= 0.0);

        return selections[selectedIndex];
    }
}
