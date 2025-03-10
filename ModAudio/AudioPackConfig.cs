using Newtonsoft.Json;

namespace Marioalexsan.ModAudio;

public class AudioPackConfig
{
    public static AudioPackConfig ReadJSON(Stream stream)
    {
        var reader = new StreamReader(stream);
        return JsonConvert.DeserializeObject<AudioPackConfig>(reader.ReadToEnd());
    }

    public static AudioPackConfig ConvertFromRoutes(RouteConfig routes)
    {
        List<ClipReplacement> replacedClips = [];

        foreach (var route in routes.ReplacedClips)
        {
            foreach (var replacement in route.Value)
            {
                replacedClips.Add(new()
                {
                    Original = route.Key,
                    Target = replacement.Name,
                    RandomWeight = replacement.RandomWeight,
                });
            }
        }

        return new AudioPackConfig
        {
            AudioClips = routes.ReplacedClips
                .SelectMany(x => x.Value)
                .Select(x => new AudioClipData()
                {
                    UniqueId = x.Name
                })
                .ToList(),
            ClipReplacements = replacedClips
        };
    }

    [JsonProperty("unique_id", Required = Required.DisallowNull)]
    public string UniqueId { get; set; } = "";

    [JsonProperty("display_name", Required = Required.DisallowNull)]
    public string DisplayName { get; set; } = "";

    public class Settings
    {
        [JsonProperty("replace_audio_clips_by_name", Required = Required.DisallowNull)]
        public bool ReplaceAudioClipsByName { get; set; } = true;
    }

    [JsonProperty("audio_pack_settings", Required = Required.DisallowNull)]
    public Settings AudioPackSettings { get; set; } = new();

    public class AudioClipData
    {
        [JsonProperty("unique_id", Required = Required.Always)]
        public string UniqueId { get; set; } = "";

        [JsonProperty("path", Required = Required.DisallowNull)]
        public string Path { get; set; } = "";

        [JsonProperty("volume_modifier", Required = Required.DisallowNull)]
        public float VolumeModifier { get; set; } = 1f;
    }

    [JsonProperty("audio_clips", Required = Required.DisallowNull)]
    public List<AudioClipData> AudioClips { get; set; } = [];

    public class ClipReplacement
    {
        [JsonProperty("original_clip", Required = Required.Always)]
        public string Original { get; set; } = "";

        [JsonProperty("target_clip", Required = Required.Always)]
        public string Target { get; set; } = "";

        [JsonProperty("random_weight", Required = Required.DisallowNull)]
        public float RandomWeight { get; set; } = 1f;
    }

    [JsonProperty("clip_replacements", Required = Required.DisallowNull)]
    public List<ClipReplacement> ClipReplacements { get; set; } = [];

    public class AudioEffect
    {
        [JsonProperty("target_clips", Required = Required.DisallowNull)]
        public List<string> TargetClips { get; set; } = [];

        [JsonProperty("target_audio_sources", Required = Required.DisallowNull)]
        public List<string> TargetAudioSources { get; set; } = [];

        [JsonProperty("volume_modifier", Required = Required.DisallowNull)]
        public float? VolumeModifier { get; set; }

        [JsonProperty("pitch_modifier", Required = Required.DisallowNull)]
        public float? PitchModifier { get; set; }
    }

    [JsonProperty("audio_effects", Required = Required.DisallowNull)]
    public List<AudioEffect> AudioEffects { get; set; } = [];

    public class PlayAudioEvent
    {
        [JsonProperty("target_clips", Required = Required.DisallowNull)]
        public List<string> TargetClips { get; set; } = [];

        [JsonProperty("target_audio_sources", Required = Required.DisallowNull)]
        public List<string> TargetAudioSources { get; set; } = [];

        [JsonProperty("trigger_chance", Required = Required.DisallowNull)]
        public float TriggerChance { get; set; } = 1f;

        public class PlayAudioEventClip
        {
            [JsonProperty("clip_name", Required = Required.Always)]
            public string ClipName { get; set; } = "";

            [JsonProperty("random_weight", Required = Required.DisallowNull)]
            public float RandomWeight { get; set; } = 1f;

            [JsonProperty("volume", Required = Required.DisallowNull)]
            public float Volume { get; set; } = 1f;

            [JsonProperty("pitch", Required = Required.DisallowNull)]
            public float Pitch { get; set; } = 1f;
        }

        [JsonProperty("clip_selection", Required = Required.Always)]
        public List<PlayAudioEventClip> ClipSelection { get; set; } = [];
    }

    [JsonProperty("play_audio_events", Required = Required.DisallowNull)]
    public List<PlayAudioEvent> PlayAudioEvents { get; set; } = [];
}
