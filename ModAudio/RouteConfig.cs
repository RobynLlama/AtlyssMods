namespace Marioalexsan.ModAudio;

public class RouteClipReplacement
{
    public string Name { get; set; } = "";
    public float RandomWeight { get; set; } = ModAudio.DefaultWeight;
}

public class RouteConfig
{
    private static readonly char[] KeyValueSeparator = ['='];
    private static readonly char[] ValueFieldSeparator = ['/'];

    public Dictionary<string, List<RouteClipReplacement>> ReplacedClips { get; set; } = [];

    public static RouteConfig ReadTextFormat(string path)
    {
        using var stream = new StreamReader(File.OpenRead(path));

        var replacedClips = new Dictionary<string, List<RouteClipReplacement>>();

        string line;
        while ((line = stream.ReadLine()) != null)
        {
            if (line.Trim() == "")
                continue; // Empty line

            if (line.TrimStart().StartsWith("#"))
                continue; // Comments

            var parts = line.Split(KeyValueSeparator, 3);

            if (parts.Length != 2)
            {
                ModAudio.Plugin?.Logger?.LogWarning($"Encountered a malformed route (expected key = value), skipping it.");
                continue;
            }

            var fields = parts[1].Split(ValueFieldSeparator, 3);

            if (fields.Length > 2)
            {
                ModAudio.Plugin?.Logger?.LogWarning($"Too many values defined for a route (expected at most 2), skipping it.");
                continue;
            }

            var clipName = parts[0].Trim();
            var replacementName = fields[0].Trim();
            var randomWeight = ModAudio.DefaultWeight;

            if (clipName.Trim() == "" || replacementName.Trim() == "")
            {
                ModAudio.Plugin?.Logger?.LogWarning($"Either clip name or replacement was empty for a route, skipping it.");
                continue;
            }

            if (fields.Length > 1)
            {
                if (!float.TryParse(fields[1], out randomWeight))
                {
                    ModAudio.Plugin?.Logger?.LogWarning($"Couldn't parse random weight {fields[1]} for {clipName} => {replacementName}, defaulting to {ModAudio.DefaultWeight}.");
                }
            }

            if (!replacedClips.TryGetValue(clipName, out var replacements))
            {
                replacedClips[clipName] = replacements = [];
            }

            replacements.Add(new()
            {
                Name = replacementName,
                RandomWeight = randomWeight
            });
        }

        return new()
        {
            ReplacedClips = replacedClips
        };
    }
}
