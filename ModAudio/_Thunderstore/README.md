# ModAudio

Replace game audio with your own mixtape.

This mod can either be used stadalone, or it can be used to make audio pack mods.

Currently, `.ogg`, `.mp3` and `.wav` formats are supported.

# Standalone Usage

To replace game audio, put your custom audio files under `BepInEx/plugins/Marioalexsan.ModAudio/audio/`.
If you're using a mod loader such as r2modman, you will have to navigate to that mod loader's BepInEx folder and put your files there.

There are two ways to do the replacement:

## Audio files named after audio clips

Name your custom audio the same as the audio clip you want to replace. For example:
- `_mu_wonton5.ogg` would replace the `_mu_wonton5` clip that is the Crescent Grove combat music
- `_mu_flyby.wav` would replace the `_mu_flyby` clip (main menu music)
- `weaponHit_Fire(average).mp3` would replace `weaponHit_Fire(average)`, i.e. one of the hit sounds

You can use any of the supported audio formats, but there should be only one format used per audio clip (i.e. if you have both `foo.ogg` and `foo.wav`, only one will be selected).

## Audio files routed to audio clips

Put your custom audio in the audio folder, the name doesn't matter in this case.
Afterwards, make an association between the audio file's name (without extensions) and the clip it's supposed to replace in the `__routes.txt` file from the `audio` folder (create it if it doesn't exist).

An example `__routes.txt` file that would map multiple hit sounds to a `pow.ogg` audio file you've placed in the `audio` folder:
```
weaponHit_Air(average) = pow
weaponHit_Air(heavy) = pow
weaponHit_Air(light) = pow
```

This can be useful to avoid duplication and to keep meaningful names for your custom audio.

# Audio Pack Mods

You can specify custom audio via mods that depend on ModAudio. Here's an example plugin that does that:

```cs
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("Marioalexsan.ModAudio")]
public class MyCustomAudio : BaseUnityPlugin
{
    private void Awake()
    {
        Marioalexsan.ModAudio.ModAudio.Instance.LoadModAudio(this);
    }
}
```

This will load audio in a similar way to how it's done for Standalone Usage, using audio files and `__routes.txt` from the `audio` folder next to your mod's assembly.

This allows you to create audio pack mods that you can distribute easily.

If you need to load audio from a different folder, you can specify it using a second parameter:

```cs
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("Marioalexsan.ModAudio")]
public class MyCustomAudio : BaseUnityPlugin
{
    private void Awake()
    {
        Marioalexsan.ModAudio.ModAudio.Instance.LoadModAudio(this, "C:\\mycoolmusicpath");
    }
}
```

# Mod Compatibility

This mod version targets ATLYSS v1.6.0b. Compatibility with other game versions is not guaranteed, especially for updates with major changes.
