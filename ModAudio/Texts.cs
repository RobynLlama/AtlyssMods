namespace Marioalexsan.ModAudio;

public static class Texts
{
    public const string VerboseLoadingTitle = "Enable verbose audio loading";
    public const string VerboseLoadingDescription = "Set to true to enable detailed logging when loading audio packs. May be useful for development or debugging purposes.";

    public const string VerboseReplacemensTitle = "Enable verbose replacements";
    public const string VerboseRoutingDescription = "Set to true to enable detailed logging related to audio replacements. May be useful for development or debugging purposes.";

    public const string OverrideCustomAudioTitle = "Override custom audio";
    public const string OverrideCustomAudioDescription = "Set to true to have ModAudio's audio clips override any custom audio from other mods. If false, it will get mixed in with other mods instead.";

    public const string ReloadTitle = "Reload audio packs";

    public static string EnablePackDescription(string displayName) => $"Set to true to enable {displayName}, false to disable it.";
}
