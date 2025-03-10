using System.IO;

namespace Marioalexsan.ModAudio;

public static class Texts
{
    public const string LogAudioLoadingTitle = "Log audio pack loading";
    public const string LogAudioLoadingDescription = "True to enable logging when loading audio packs,  false to disable it.";

    public const string LogCustomAudioTitle = "Log custom audio";
    public const string LogCustomAudioDescription = "True to enable logging when audio is replaced with custom tracks, false to disable it.";

    public const string OverrideCustomAudioTitle = "Override custom audio";
    public const string OverrideCustomAudioDescription = "If true, and the user audio pack in ModAudio's config has custom audio, that custom audio will override any custom audio for the same track coming from downloaded packs.";

    public const string ReloadTitle = "Reload audio packs";

    public static string WeightClamped(float weight, AudioPackConfig.ClipReplacement replacement)
        => $"Weight {weight} for {replacement.Original} => {replacement.Target} is outside the [{ModAudio.MinWeight}, {ModAudio.MaxWeight}] range and was clamped.";

    public static string EnablePackDescription(string displayName)
        => $"Set to true to enable {displayName}, false to disable it.";

    public static string InvalidPackId(string path, string id)
        => $"Refusing to load pack {id} from {path}, its ID contains invalid characters.";

    public static string DuplicatePackId(string path, string id)
        => $"Refusing to load pack {id} from {path}, an audio pack with that ID already exists.";

    public static string DuplicateClipId(string path, string id)
        => $"Refusing to load clip {id} from {path}, a clip with that name was already loaded.";

    public static string InvalidPackPath(string path, string id)
        => $"Refusing to load clip {id} from {path}, its path was outside of audio pack.";

    public static string DuplicateClipSkipped(string file, string id)
        => $"Audio file {file} was not loaded, clip {id} was already loaded previously.";

    public static string AudioCannotBeStreamed(string path, long fileSize)
        => $"Audio file {path} is above the size threshold for streaming" +
           $" ({fileSize / AudioPackLoader.OneMB}MB >= {AudioPackLoader.FileSizeLimitForLoading / AudioPackLoader.OneMB}MB)," +
           $" but it cannot be streamed using the current audio format." +
           $" Please try using one of the following supported formats: {string.Join(", ", AudioClipLoader.SupportedStreamExtensions)}.";
}
