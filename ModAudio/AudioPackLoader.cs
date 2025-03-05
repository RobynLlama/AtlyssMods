using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace Marioalexsan.ModAudio;

public static class AudioPackLoader
{
    private const float OneMB = 1024f * 1024f;
    private static ManualLogSource Logger => ModAudio.Plugin.Logger;

    public const string AudioPackConfigName = "modaudio.config.json";
    public const string RoutesConfigName = "__routes.txt";
    public const int FileSizeLimitForLoading = 1024 * 1024;

    private static string ReplaceRootPath(string path)
    {
        return Path.GetFullPath(path)
            .Replace($"{Paths.PluginPath}/", "plugin://")
            .Replace($"{Paths.PluginPath}\\", "plugin://")
            .Replace($"{Paths.ConfigPath}/", "config://")
            .Replace($"{Paths.ConfigPath}\\", "config://");
    }

    private static bool IsNormalizedId(string id)
    {
        return !id.Contains("\\");
    }

    private static string NormalizeId(string id)
    {
        return id
            .Replace("\\", "/");
    }

    private static string ConvertPathToId(string path)
    {
        return NormalizeId(ReplaceRootPath(path));
    }

    private static string ConvertPathToDisplayName(string path)
    {
        var cleanPath = Path.GetFullPath(path)
            .Replace($"{Paths.PluginPath}/", "")
            .Replace($"{Paths.PluginPath}\\", "")
            .Replace($"{Paths.ConfigPath}/", "")
            .Replace($"{Paths.ConfigPath}\\", "")
            .Replace("\\", "/");

        var index = cleanPath.IndexOf('/');

        return index == -1 ? cleanPath : cleanPath.Substring(0, index);
    }

    public static List<AudioPack> LoadAudioPacks()
    {
        List<AudioPack> audioPacks = [];
        List<string> loadPaths = [
            ..Directory.GetDirectories(Paths.ConfigPath),
            ..Directory.GetDirectories(Paths.PluginPath),
        ];

        foreach (var rootPath in loadPaths)
        {
            LoadAudioPacksFromRoot(audioPacks, rootPath);
        }

        foreach (var audioPack in audioPacks)
        {
            FinalizePack(audioPack);
        }

        return audioPacks;
    }

    private static void FinalizePack(AudioPack pack)
    {
        // Validate / normalize / remap stuff

        foreach (var replacement in pack.Config.ClipReplacements)
        {
            if (!pack.Replacements.ContainsKey(replacement.Original))
            {
                pack.Replacements[replacement.Original] = [];
            }

            pack.Replacements[replacement.Original].Add(replacement);
        }

        foreach (var replacement in pack.Replacements)
        {
            foreach (var branch in replacement.Value)
            {
                var randomWeight = branch.RandomWeight;

                if (randomWeight < ModAudio.MinWeight)
                {
                    Logger.LogWarning($"Weight {randomWeight} for {branch.Original} => {branch.Target} is too low and was capped to {ModAudio.MinWeight}.");
                    randomWeight = ModAudio.MinWeight;
                }

                if (randomWeight > ModAudio.MaxWeight)
                {
                    Logger.LogWarning($"Weight {randomWeight} for {branch.Original} => {branch.Target} is too high and was capped to {ModAudio.MinWeight}.");
                    randomWeight = ModAudio.MaxWeight;
                }

                branch.RandomWeight = randomWeight;
            }
        }
    }

    private static void LoadAudioPacksFromRoot(List<AudioPack> packs, string rootPath)
    {
        Queue<string> paths = new();
        paths.Enqueue(rootPath);

        while (paths.Count > 0)
        {
            var path = paths.Dequeue();

            foreach (var directory in Directory.GetDirectories(path))
            {
                paths.Enqueue(directory);
            }

            // Check for the existence of an audio pack config
            var audioPackConfigPath = Path.Combine(path, AudioPackConfigName);
            if (File.Exists(audioPackConfigPath))
            {
                var pack = LoadAudioPack(packs, audioPackConfigPath);

                if (pack != null)
                {
                    Logger.LogInfo($"Loaded audio pack {pack.Config.UniqueId} ({ReplaceRootPath(pack.PackPath)})");
                    packs.Add(pack);
                }

                continue;
            }

            // (legacy) Check for the existence of a routes file
            var routesConfigPath = Path.Combine(path, RoutesConfigName);
            if (File.Exists(routesConfigPath))
            {
                var pack = LoadLegacyAudioPack(packs, routesConfigPath);

                if (pack != null)
                {
                    Logger.LogInfo($"Loaded legacy audio pack {pack.Config.UniqueId} ({ReplaceRootPath(pack.PackPath)})");
                    packs.Add(pack);
                }

                continue;
            }

            // (legacy) Check for the existence of audio clips under "/" or "/audio/"
            if ((path == rootPath || path == Path.Combine(rootPath, "audio")))
            {
                if (Directory.GetFiles(path).Any(x => VanillaClipNames.IsKnownClip(Path.GetFileNameWithoutExtension(x))))
                {
                    var pack = LoadLegacyAudioPack(packs, routesConfigPath);

                    if (pack != null)
                    {
                        Logger.LogInfo($"Loaded legacy audio pack {pack.Config.UniqueId} ({ReplaceRootPath(pack.PackPath)})");
                        packs.Add(pack);
                    }

                    continue;
                }
            }
        }
    }

    private static AudioPack LoadAudioPack(List<AudioPack> existingPacks, string path)
    {
        using var stream = File.OpenRead(path);
        AudioPackConfig config = AudioPackConfig.ReadJSON(stream);

        AudioPack pack = new()
        {
            Config = config,
            PackPath = path
        };


        if (string.IsNullOrEmpty(pack.Config.UniqueId))
        {
            // Assign an ID based on location
            pack.Config.UniqueId = ConvertPathToId(pack.PackPath);
        }
        else if (!IsNormalizedId(pack.Config.UniqueId))
        {
            Logger.LogWarning($"Refusing to load pack from {path}, the ID {pack.Config.UniqueId} contains invalid characters.");
            return null;
        }

        if (string.IsNullOrEmpty(pack.Config.DisplayName))
        {
            // Assign a display name based on folder
            pack.Config.DisplayName = ConvertPathToDisplayName(pack.PackPath);
        }

        if (existingPacks.Any(x => x.Config.UniqueId == pack.Config.UniqueId))
        {
            Logger.LogWarning($"Refusing to load pack from {path}, an audio pack with ID {pack.Config.UniqueId} already exists.");
            return null;
        }

        var rootPath = Path.GetFullPath(Path.GetDirectoryName(path));

        foreach (var clipData in config.AudioClips)
        {
            if (pack.LoadedClips.Any(x => x.Value.name == clipData.UniqueId))
            {
                Logger.LogWarning($"Refusing to load clip {clipData.UniqueId} from {clipData.Path}, a clip with that name was already loaded.");
                continue;
            }

            var clipPath = Path.GetFullPath(Path.Combine(rootPath, clipData.Path));

            if (!clipPath.StartsWith(rootPath))
            {
                Logger.LogWarning($"Refusing to load clip {clipData.UniqueId}, its path was outside of audio pack: {clipData.Path}.");
                continue;
            }

            bool isAudioFile = AudioClipLoader.SupportedStreamExtensions.Any(clipPath.EndsWith) || AudioClipLoader.SupportedLoadExtensions.Any(clipPath.EndsWith);

            if (!isAudioFile)
                continue;

            long fileSize = new FileInfo(clipPath).Length;
            bool useStreaming = fileSize >= FileSizeLimitForLoading;

            if (useStreaming && !AudioClipLoader.SupportedStreamExtensions.Any(clipPath.EndsWith))
            {
                useStreaming = false;
                Logger.LogWarning(
                    $"Audio file {clipPath} is a big audio file ({fileSize / OneMB}MB >= {FileSizeLimitForLoading / OneMB}MB), but it cannot be streamed using the current audio format." +
                    $" Please try using one of the following supported formats for better performance: {string.Join(", ", AudioClipLoader.SupportedStreamExtensions)}"
                );
            }

            try
            {
                var clip = useStreaming ? AudioClipLoader.StreamFromFile(clipData.UniqueId, clipPath) : AudioClipLoader.LoadFromFile(clipData.UniqueId, clipPath);
                pack.LoadedClips[clip.name] = clip;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to load {clipData.UniqueId} from {clipPath}!");
                Logger.LogWarning($"Exception: {e}");
            }
        }

        return pack;
    }

    private static AudioPack LoadLegacyAudioPack(List<AudioPack> existingPacks, string path)
    {
        var id = NormalizeId(ReplaceRootPath(path));

        if (existingPacks.Any(x => x.Config.UniqueId == id))
        {
            Logger.LogWarning($"Refusing to load pack from {path}, an audio pack with ID {id} already exists.");
            return null;
        }

        AudioPack pack = new()
        {
            PackPath = path,
        };

        // Add explicit routes
        if (File.Exists(path))
        {
            var routes = RouteConfig.ReadTextFormat(path);
            pack.Config = AudioPackConfig.ConvertFromRoutes(routes);
        }

        pack.Config.DisplayName = ConvertPathToDisplayName(path);
        pack.Config.UniqueId = ConvertPathToId(path);

        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(path)))
        {
            bool isAudioFile = AudioClipLoader.SupportedStreamExtensions.Any(file.EndsWith) || AudioClipLoader.SupportedLoadExtensions.Any(file.EndsWith);

            if (!isAudioFile)
                continue;

            long fileSize = new FileInfo(file).Length;
            bool useStreaming = fileSize >= FileSizeLimitForLoading;

            if (useStreaming && !AudioClipLoader.SupportedStreamExtensions.Any(file.EndsWith))
            {
                useStreaming = false;
                Logger.LogWarning(
                    $"Audio file {file} is a big audio file ({fileSize / OneMB}MB >= {FileSizeLimitForLoading / OneMB}MB), but it cannot be streamed using the current audio format." +
                    $" Please try using one of the following supported formats for better performance: {string.Join(", ", AudioClipLoader.SupportedStreamExtensions)}"
                );
            }

            var name = Path.GetFileNameWithoutExtension(file);

            if (pack.LoadedClips.ContainsKey(name))
            {
                Logger.LogWarning($"Audio file {file} was not loaded, clip {name} was already loaded previously.");
                continue;
            }

            if (ModAudio.Plugin.VerboseLoading.Value)
                Logger.LogInfo($"Loading {name} from {file} with streaming set to {useStreaming}...");

            try
            {
                var clip = useStreaming ? AudioClipLoader.StreamFromFile(name, file) : AudioClipLoader.LoadFromFile(name, file);
                pack.LoadedClips[name] = clip;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to load {name} from {file}!");
                Logger.LogWarning($"Exception: {e}");
            }
        }

        // Add implicit routes
        foreach (var clip in pack.LoadedClips)
        {
            pack.Config.ClipReplacements.Add(new()
            {
                Original = clip.Key,
                Target = clip.Key,
                RandomWeight = 1f
            });
        }

        return pack;
    }
}
