# ModAudio

Replace game audio with your own mixtape.

You can use this mod to make audio pack mods, or to just replace audio on a whim.

Currently, `.ogg`, `.mp3` and `.wav` formats are supported, although `.ogg` and `.mp3` are preferred due to reduced file size.

# How to use

ModAudio loads custom audio and routing information from any custom mods you have in the plugins folder (`BepInEx/plugins/YourTeam-YourModName/audio`).

It uses a combination of audio files and the `__routes.txt` configuration file located in the audio folder of a mod.

It also loads custom audio from its own folder (`BepInEx/plugins/Marioalexsan-ModAudio/audio`). You can place your custom audio in here if you don't want to make a standalone mod.

# Where is this audio folder located???

When using r2modman, you can find BepInEx in your profile folder for ATLYSS ("Settings" button, first option in the list). For example: `C:\Users\USERNAME\AppData\Roaming\r2modmanPlus-local\ATLYSS\profiles\Default`.

If you've installed BepInEx manually, then go to Steam's ATLYSS install path. For example: `C:\Program Files (x86)\Steam\steamapps\common\ATLYSS`.

From the r2modman profile or the Steam install, navigate to `BepInEx/plugins/Marioalexsan-ModAudio/audio`, or your custom mod's audio folder.

# Audio mod example

*Note: There are already plenty of audio mods created that use ModAudio on Thunderstore. If something in here is confusing, you can do "Manual download" for those audio mods to take a look at how they package their assets.*

Here's an example of an audio mod that uses all of the features combined:

**mod folder structure under BepInEx/plugins**
```
ModFolder
|- audio
   |- __routes.txt
   |- _mu_flyby.mp3
   |- darkest_dungeon_combat.mp3
   |- risk_of_rain_2_boss.wav
   |- team_fortress_2_loadout.ogg
   |- cod_hitsound_01.mp3
   |- cod_hitsound_02.mp3
   |- cod_hitsound_03.mp3
```

**__routes.txt file contents**
```
# Empty lines and lines that start with a hashtag are ignored

# Musics
_mu_wonton5 = darkest_dungeon_combat
_mu_ekca = risk_of_rain_2_boss
_mu_selee = team_fortress_2_loadout

# Hit sounds
weaponHit_Normal(average) = cod_hitsound_01
weaponHit_Normal(average) = cod_hitsound_02 / 0.5
weaponHit_Normal(average) = cod_hitsound_03 / 0.5
weaponHit_Normal(average) = ___default___ / 2
```

This audio mod will do the following:
- play `_mu_flyby.mp3` instead of `_mu_flyby` (main menu music) - implicitly because the file name matches the clip
- play `darkest_dungeon_combat.mp3` instead of `_mu_wonton5` (Grove combat)
- play `risk_of_rain_2_boss.wav` instead of `_mu_ekca` (Grove boss music)
- play `team_fortress_2_loadout.ogg` instead of `_mu_selee` (Character selection music)
- play one of `cod_hitsound_01.mp3`, `cod_hitsound_02.mp3` or `cod_hitsound_03.mp3` for Normal type average weapon hits
  - the total weight for this is `1 (implicit) + 0.5 + 0.5 + 2 = 4`
  - `cod_hitsound_01.mp3` will play with a `1 / 4 * 100% = 25%` chance
  - `cod_hitsound_02.mp3` will play with a `0.5 / 5 * 100% = 12.5%` chance
  - `cod_hitsound_03.mp3` will play with a `0.5 / 5 * 100% = 12.5%` chance
  - the original, unmodified sound clip will play with a `2 / 4 * 100% = 50%` chance

# How does this work???

## Direct replacement

You can replace audio clips directly if the file name matches the clip name.

1. Choose a clip you want to replace (for example, `_mu_wonton5` - Crescent Grove's action music).
2. Take your custom audio, and rename it so that it has the same name as the clip (for example, `coolmusic.mp3` -> `_mu_wonton5.mp3`).
3. Place the audio file in the `audio` folder.

## Reroute the clip in __routes.txt

You can do custom replacements by specifying replacement information in the `__routes.txt` file.

1. Choose a clip you want to replace (for example, `_mu_wonton5` - Crescent Grove's action music).
2. Take your custom audio, and put it in the `audio` folder (for example, `coolmusic.mp3`). The audio file can have any name you want.
3. Create a `__routes.txt` file in the `audio` folder, or open it with Notepad if it already exists.
4. Add the clip name and the file name without the extension, separeted by `=` on a new line : `_mu_wonton5 = coolmusic`.

This will tell ModAudio to play `coolmusic.mp3` every time `_mu_wonton5` would play in the game.

If you add multiple lines that have use the same clip, then ModAudio will play one of them randomly.

For example, `firstbossmusic.mp3` and `secondbossmusic.mp3` will each play about half of the time:

```
_mu_wonton5 = firstbossmusic
_mu_wonton5 = secondbossmusic
```

If you want clips to play with different chances, you can specify a number as a weight for the random roll. Separate it from the audio file name with a `/`.

For example, `firstbossmusic` will play 2/3rds of the time (1.0 / (1.0 + 0.5)), and `secondbossmusic` will play 1/3rd of the time (0.5 / (1.0 + 0.5)):

```
_mu_wonton5 = firstbossmusic / 1.0
_mu_wonton5 = secondbossmusic / 0.5
```

If you don't specify a weight, then it defaults to 1.

Finally, if you want to tell ModAudio to play the original clip, you can use the special `___default___` identifier instead of a file name:

```
_mu_wonton5 = ___default___ / 0.8
_mu_wonton5 = _mu_wonton5_remix / 0.2
```

This will play the default boss music with an 80% chance, and a remixed version with a 20% chance.

# Packaging your audio mods for Thunderstore / r2modman

When packaging your mods for Thunderstore / r2modman, you need to put your `audio` folder that contains your audio and `__routes.txt` under the `plugins` folder in the zip.

This is to make sure that r2modman won't flatten all of your files into the root directory, which might cause issues.

Here's an example of how your ZIP package should look like:

***yourmod.zip***
```
|- manifest.json
|- README.md
|- CHANGELOG.md
|- icon.png
|- plugins
   |- audio
      |- __routes.txt
      |- _mu_flyby.mp3
      |- someaudio.ogg
      |- someotheraudio.wav
```

r2modman will take all of your content from the ZIP's `plugins` and put it as-is in the mod folder, thus preserving the folder structure that ModAudio wants.

Also, your manifest.json should have ModAudio listed as a dependency, with the latest version being preferable:

***manifest.json***
```
{
  "name": "YourModName",
  "description": "Cool sounds and stuff",
  "version_number": "1.0.0",
  "dependencies": [
    "BepInEx-BepInExPack-5.4.2100",
    "Marioalexsan-ModAudio-1.1.0"
  ],
  "website_url": "https://github.com/Marioalexsan/AtlyssMods"
}
```

*Do not include the ModAudio DLL in your own mod.* It's not needed, and it might cause issues with loading. You just need the dependency string in manifest.json.

# Advanced usage

## Multiple audio mods

If you use multiple audio mods that replace the same audio clips, ModAudio will effectively combine them into one.

For example, if you have two mods that replace the Main Menu music, then each of them will have a 50% chance to play.

If the first mod's replacement has a weight of 1, and the second one has a weight of two, then it will be a 33% / 67% chance split for either of them to play.

## Custom audio folder location

If you are making a custom plugin and need to load audio information from a different folder, you can use the `ModAudio.LoadModAudio()` method.

For example, this will tell ModAudio to load audio from `C:\\mycoolmusicpath` for your mod, instead of trying to read the `audio` folder next to the DLL.
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

## Audio override

In the mod's configuration file (`BepInEx/config/Marioalexsan.ModAudio.cfg`), you can set `OverrideCustomAudio` to `true` to enable audio overrides.

This will cause **anything** you place under `BepInEx/plugins/ModAudio/audio` to completely override what any other mods are trying to do.

For example, if there is a mod that replaces a lot of sounds, but you want to keep the main menu music intact, you can enable that option and specify this in `__routes.txt`:

```
_mu_flyby = ___default___
```

This will make it play the vanilla music every time, ignoring whatever other mods have defined.

## Extensive logging

Set `ExtensiveLogging` to `true` in the configuration file to have ModAudio log sounds played, sounds replaced, and lots of other debug information.

This is spammy, but it can be useful to debug what is happening with your mods, or find out what sounds are being played in the world.

The logging is done to BepInEx's console and `BepInEx/LogOutput.log`.

# Mod Compatibility

This mod version targets ATLYSS v1.6.2b. Compatibility with other game versions is not guaranteed, especially for updates with major changes.
