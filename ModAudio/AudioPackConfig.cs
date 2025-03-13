using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Marioalexsan.ModAudio;

public class AudioPackConfig
{
    [JsonProperty("id", Required = Required.DisallowNull)]
    public string Id { get; set; } = "";

    [JsonProperty("display_name", Required = Required.DisallowNull)]
    public string DisplayName { get; set; } = "";

    public class Settings
    {
        [JsonProperty("autoload_replacement_clips", Required = Required.DisallowNull)]
        public bool AutoloadReplacementClips { get; set; } = true;
    }

    [JsonProperty("settings", Required = Required.DisallowNull)]
    public Settings AudioPackSettings { get; set; } = new();

    public class AudioClipData
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = "";

        [JsonProperty("path", Required = Required.DisallowNull)]
        public string Path { get; set; } = "";

        [JsonProperty("volume", Required = Required.DisallowNull)]
        public float Volume { get; set; } = 1f;
    }

    [JsonProperty("custom_clips", Required = Required.DisallowNull)]
    public List<AudioClipData> CustomClips { get; set; } = [];

    public class ClipReplacement
    {
        [JsonProperty("original", Required = Required.Always)]
        public string Original { get; set; } = "";

        [JsonProperty("replacement", Required = Required.Always)]
        public string Target { get; set; } = "";

        [JsonProperty("weight", Required = Required.DisallowNull)]
        public float RandomWeight { get; set; } = 1f;
    }

    [JsonProperty("clip_replacements", Required = Required.DisallowNull)]
    public List<ClipReplacement> Replacements { get; set; } = [];

    public class PlayAudioEvent
    {
        [JsonProperty("target_clips", Required = Required.DisallowNull)]
        public List<string> TargetClips { get; set; } = [];

        [JsonProperty("target_sources", Required = Required.DisallowNull)]
        public List<string> TargetSources { get; set; } = [];

        [JsonProperty("target_prefabs", Required = Required.DisallowNull)]
        public List<string> TargetPrefabs { get; set; } = [];

        [JsonProperty("trigger_chance", Required = Required.DisallowNull)]
        public float TriggerChance { get; set; } = 1f;

        public class PlayAudioEventClip
        {
            [JsonProperty("clip_name", Required = Required.Always)]
            public string ClipName { get; set; } = "";

            [JsonProperty("weight", Required = Required.DisallowNull)]
            public float RandomWeight { get; set; } = 1f;

            [JsonProperty("volume", Required = Required.DisallowNull)]
            public float Volume { get; set; } = 1f;

            [JsonProperty("pitch", Required = Required.DisallowNull)]
            public float Pitch { get; set; } = 1f;
        }

        [JsonProperty("clips", Required = Required.Always)]
        public List<PlayAudioEventClip> ClipSelection { get; set; } = [];
    }

    [JsonProperty("audio_overlays", Required = Required.DisallowNull)]
    public List<PlayAudioEvent> PlayAudioEvents { get; set; } = [];

    public class AudioEffect
    {
        [JsonProperty("target_clips", Required = Required.DisallowNull)]
        public List<string> TargetClips { get; set; } = [];

        [JsonProperty("target_sources", Required = Required.DisallowNull)]
        public List<string> TargetSources { get; set; } = [];

        [JsonProperty("volume", Required = Required.DisallowNull)]
        public float? Volume { get; set; }

        [JsonProperty("pitch", Required = Required.DisallowNull)]
        public float? Pitch { get; set; }
    }

    [JsonProperty("effects", Required = Required.DisallowNull)]
    public List<AudioEffect> Effects { get; set; } = [];

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
            CustomClips = routes.ReplacedClips
                .SelectMany(x => x.Value)
                .Select(x => new AudioClipData()
                {
                    Name = x.Name
                })
                .ToList(),
            Replacements = replacedClips
        };
    }

    public static string GenerateSchema()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return new JsonSchemaGenerator().Generate(typeof(AudioPackConfig)).ToString();
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
