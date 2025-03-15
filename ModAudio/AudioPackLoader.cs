using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace Marioalexsan.ModAudio;

public static class AudioPackLoader
{
    public const float OneMB = 1024f * 1024f;

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
        return id.Replace("\\", "/");
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
            Logging.LogInfo($"Loaded pack {audioPack.Config.Id} | Routes {audioPack.Config.Routes.Count} | Loaded clips {audioPack.LoadedClips.Count} | Streamed clips {audioPack.DelayedLoadClips.Count}");
        }

        return audioPacks;
    }

    private static void FinalizePack(AudioPack pack)
    {
        // Validate / normalize / remap stuff

        foreach (var route in pack.Config.Routes)
        {
            var clampedWeight = Mathf.Clamp(route.ReplacementWeight, ModAudio.MinWeight, ModAudio.MaxWeight);

            if (clampedWeight != route.ReplacementWeight)
                Logging.LogWarning(Texts.WeightClamped(clampedWeight, pack));

            route.ReplacementWeight = clampedWeight;

            foreach (var selection in route.ReplacementClips)
            {
                clampedWeight = Mathf.Clamp(selection.Weight, ModAudio.MinWeight, ModAudio.MaxWeight);

                if (clampedWeight != selection.Weight)
                    Logging.LogWarning(Texts.WeightClamped(clampedWeight, pack));

                selection.Weight = clampedWeight;
            }

            foreach (var selection in route.OverlayClips)
            {
                clampedWeight = Mathf.Clamp(selection.Weight, ModAudio.MinWeight, ModAudio.MaxWeight);

                if (clampedWeight != selection.Weight)
                    Logging.LogWarning(Texts.WeightClamped(clampedWeight, pack));

                selection.Weight = clampedWeight;
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
                        packs.Add(pack);
                    }

                    continue;
                }
            }
        }
    }

    private static AudioPack LoadAudioPack(List<AudioPack> existingPacks, string path)
    {
        Logging.LogInfo($"Loading audio pack from {path}", ModAudio.Plugin.LogAudioLoading);
        using var stream = File.OpenRead(path);
        AudioPackConfig config = AudioPackConfig.ReadJSON(stream);

        AudioPack pack = new()
        {
            Config = config,
            PackPath = path
        };

        if (string.IsNullOrEmpty(pack.Config.Id))
        {
            // Assign an ID based on location
            pack.Config.Id = ConvertPathToId(pack.PackPath);
        }
        else if (!IsNormalizedId(pack.Config.Id))
        {
            Logging.LogWarning(Texts.InvalidPackId(pack.PackPath, pack.Config.Id));
            return null;
        }

        if (string.IsNullOrEmpty(pack.Config.DisplayName))
        {
            // Assign a display name based on folder
            pack.Config.DisplayName = ConvertPathToDisplayName(pack.PackPath);
        }

        if (existingPacks.Any(x => x.Config.Id == pack.Config.Id))
        {
            Logging.LogWarning(Texts.DuplicatePackId(pack.PackPath, pack.Config.Id));
            return null;
        }

        var rootPath = Path.GetFullPath(Path.GetDirectoryName(path));

        LoadCustomClips(rootPath, pack, false);

        if (pack.Config.AudioPackSettings.AutoloadReplacementClips)
        {
            Logging.LogInfo($"Autoloading replacement clips from {path}", ModAudio.Plugin.LogAudioLoading);

            AutoLoadReplacementClipsFromPath(path, pack);
        }

        return pack;
    }

    private static AudioPack LoadLegacyAudioPack(List<AudioPack> existingPacks, string path)
    {
        Logging.LogInfo($"Loading legacy pack from {path}", ModAudio.Plugin.LogAudioLoading);
        var id = NormalizeId(ReplaceRootPath(path));

        if (existingPacks.Any(x => x.Config.Id == id))
        {
            Logging.LogWarning(Texts.DuplicatePackId(path, id));
            return null;
        }

        AudioPack pack = new()
        {
            PackPath = path,
            Config =
            {
                AudioPackSettings =
                {
                    AutoloadReplacementClips = true
                }
            }
        };

        // Add explicit routes
        if (File.Exists(path))
        {
            var routes = RouteConfig.ReadTextFormat(path);
            pack.Config = AudioPackConfig.ConvertFromRoutes(routes);
        }

        pack.Config.DisplayName = ConvertPathToDisplayName(path);
        pack.Config.Id = ConvertPathToId(path);

        var rootPath = Path.GetFullPath(Path.GetDirectoryName(path));

        LoadCustomClips(rootPath, pack, true);
        AutoLoadReplacementClipsFromPath(path, pack);

        return pack;
    }

    private static void LoadCustomClips(string rootPath, AudioPack pack, bool extensionless)
    {
        foreach (var clipData in pack.Config.CustomClips)
        {
            if (pack.LoadedClips.Any(x => x.Value.name == clipData.Name))
            {
                Logging.LogWarning(Texts.DuplicateClipId(clipData.Path, clipData.Name));
                continue;
            }

            var clipPath = Path.GetFullPath(Path.Combine(rootPath, clipData.Path));

            if (!clipPath.StartsWith(rootPath))
            {
                Logging.LogWarning(Texts.InvalidPackPath(clipData.Path, clipData.Name));
                continue;
            }

            if (clipData.IgnoreClipExtension)
            {
                // Search for a file that is supported
                foreach (var ext in AudioClipLoader.SupportedStreamExtensions)
                {
                    if (File.Exists(clipPath + ext))
                    {
                        clipPath = clipPath + ext;
                        break;
                    }
                }
            }

            bool isAudioFile = AudioClipLoader.SupportedExtensions.Any(clipPath.EndsWith);

            if (!isAudioFile)
                continue;

            long fileSize = new FileInfo(clipPath).Length;
            bool useStreaming = fileSize >= FileSizeLimitForLoading;

            if (useStreaming && !AudioClipLoader.SupportedStreamExtensions.Any(clipPath.EndsWith))
            {
                Logging.LogWarning(Texts.AudioCannotBeStreamed(clipPath, fileSize));
                useStreaming = false;
            }

            try
            {
                Logging.LogInfo($"Loading {(useStreaming ? "streamed " : "")}clip {clipData.Name} from {clipPath}", ModAudio.Plugin.LogAudioLoading);

                if (useStreaming)
                {
                    // Opening a ton of streams at the start is not great, plus it adds a sizeable amount of load time if you have a lot of packs
                    pack.DelayedLoadClips[clipData.Name] = () => AudioClipLoader.StreamFromFile(clipData.Name, clipPath, clipData.Volume);
                }
                else
                {
                    pack.LoadedClips[clipData.Name] = AudioClipLoader.LoadFromFile(clipData.Name, clipPath, clipData.Volume);
                }
            }
            catch (Exception e)
            {
                Logging.LogWarning($"Failed to load {clipData.Name} from {clipPath}!");
                Logging.LogWarning($"Exception: {e}");
            }
        }
    }

    private static void AutoLoadReplacementClipsFromPath(string path, AudioPack pack)
    {
        List<string> detectedClips = [];

        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(path)))
        {
            bool isAudioFile = AudioClipLoader.SupportedExtensions.Any(file.EndsWith);

            if (!isAudioFile || !VanillaClipNames.IsKnownClip(Path.GetFileNameWithoutExtension(path)))
                continue;

            long fileSize = new FileInfo(file).Length;
            bool useStreaming = fileSize >= FileSizeLimitForLoading;

            if (useStreaming && !AudioClipLoader.SupportedStreamExtensions.Any(file.EndsWith))
            {
                Logging.LogWarning(Texts.AudioCannotBeStreamed(file, fileSize));
                useStreaming = false;
            }

            var name = Path.GetFileNameWithoutExtension(file);

            if (pack.LoadedClips.ContainsKey(name))
            {
                Logging.LogWarning(Texts.DuplicateClipSkipped(file, name));
                continue;
            }

            detectedClips.Add(name);

            try
            {
                Logging.LogInfo($"Loading {(useStreaming ? "streamed " : "")}clip {name} from {file}", ModAudio.Plugin.LogAudioLoading);

                if (useStreaming)
                {
                    // Opening a ton of streams at the start is not great, plus it adds a sizeable amount of load time if you have a lot of packs
                    pack.DelayedLoadClips[name] = () => AudioClipLoader.StreamFromFile(name, file, 1f);
                }
                else
                {
                    pack.LoadedClips[name] = AudioClipLoader.LoadFromFile(name, file, 1f);
                }
            }
            catch (Exception e)
            {
                Logging.LogWarning($"Failed to load {name} from {file}!");
                Logging.LogWarning($"Exception: {e}");
            }
        }

        // Add implicit routes
        foreach (var clip in detectedClips)
        {
            pack.Config.Routes.Add(new()
            {
                OriginalClips = [clip],
                ReplacementClips = [new AudioPackConfig.Route.ClipSelection() {
                    Name = clip,
                }],
            });
        }
    }
}
