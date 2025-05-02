namespace Marioalexsan.AtlyssDiscordRichPresence;

using BepInEx.Configuration;
using static States;

public class Display
{
    public enum DisplayPresets
    {
        Custom,
        Default,
        Emojis
    }

    public enum Texts
    {
        // Full texts
        PlayerAlive,
        PlayerDead,
        MainMenu,
        Exploring,
        Idle,
        FightingInArena,
        FightingBoss,
        Singleplayer,
        Multiplayer,

        // Race variables
        RaceUnknown,
        RaceImp,
        RacePoon,
        RaceKubold,
        RaceByrdle,
        RaceChang,

        // Class variables
        ClassUnknown,
        ClassNovice,
        ClassFighter,
        ClassMystic,
        ClassBandit
    }

    public Display(ConfigFile config)
    {
        DisplayPreset = config.Bind("General", nameof(DisplayPreset), DisplayPresets.Custom, "Preset to use for texts. \"Custom\" will use the custom texts defined in the config.");

        PlayerAliveCustom = config.Bind("Display", "PlayerAlive", GetDefaultPresetText(Texts.PlayerAlive), "Text to display for player stats while alive.");
        PlayerDeadCustom = config.Bind("Display", "PlayerDead", GetDefaultPresetText(Texts.PlayerDead), "Text to display for player stats while dead.");
        MainMenuCustom = config.Bind("Display", "MainMenu", GetDefaultPresetText(Texts.MainMenu), "Text to display while you're in the main menu.");
        ExploringCustom = config.Bind("Display", "Exploring", GetDefaultPresetText(Texts.Exploring), "Text to display while exploring the world.");
        IdleCustom = config.Bind("Display", "Idle", GetDefaultPresetText(Texts.Idle), "Text to display while being idle in the world.");
        FightingInArenaCustom = config.Bind("Display", "FightingInArena", GetDefaultPresetText(Texts.FightingInArena), "Text to display while an arena is active.");
        FightingBossCustom = config.Bind("Display", "FightingBoss", GetDefaultPresetText(Texts.FightingBoss), "Text to display while a boss is active.");
        SingleplayerCustom = config.Bind("Display", "Singleplayer", GetDefaultPresetText(Texts.Singleplayer), "Text to display while in singleplayer.");
        MultiplayerCustom = config.Bind("Display", "Multiplayer", GetDefaultPresetText(Texts.Multiplayer), "Text to display while in multiplayer.");

        RaceImpCustom = config.Bind("Display", "RaceImp", GetDefaultPresetText(Texts.RaceImp), $"Text to use when displaying {PLAYERRACE} (for Imps).");
        RacePoonCustom = config.Bind("Display", "RacePoon", GetDefaultPresetText(Texts.RacePoon), $"Text to use when displaying {PLAYERRACE} (for Poons).");
        RaceKuboldCustom = config.Bind("Display", "RaceKubold", GetDefaultPresetText(Texts.RaceKubold), $"Text to use when displaying {PLAYERRACE} (for Kubolds).");
        RaceByrdleCustom = config.Bind("Display", "RaceByrdle", GetDefaultPresetText(Texts.RaceByrdle), $"Text to use when displaying {PLAYERRACE} (for Byrdles).");
        RaceChangCustom = config.Bind("Display", "RaceChang", GetDefaultPresetText(Texts.RaceChang), $"Text to use when displaying {PLAYERRACE} (for Changs).");

        ClassNoviceCustom = config.Bind("Display", "ClassNovice", GetDefaultPresetText(Texts.ClassNovice), $"Text to use when displaying {PLAYERCLASS} (for Novices).");
        ClassFighterCustom = config.Bind("Display", "ClassFighter", GetDefaultPresetText(Texts.ClassFighter), $"Text to use when displaying {PLAYERCLASS} (for Fighters).");
        ClassMysticCustom = config.Bind("Display", "ClassMystic", GetDefaultPresetText(Texts.ClassMystic), $"Text to use when displaying {PLAYERCLASS} (for Mystics).");
        ClassBanditCustom = config.Bind("Display", "ClassBandit", GetDefaultPresetText(Texts.ClassBandit), $"Text to use when displaying {PLAYERCLASS} (for Bandits).");
    }

    public ConfigEntry<DisplayPresets> DisplayPreset { get; }

    // Full texts (Custom)
    public ConfigEntry<string> PlayerAliveCustom { get; }
    public ConfigEntry<string> PlayerDeadCustom { get; }
    public ConfigEntry<string> MainMenuCustom { get; }
    public ConfigEntry<string> ExploringCustom { get; }
    public ConfigEntry<string> IdleCustom { get; }
    public ConfigEntry<string> FightingInArenaCustom { get; }
    public ConfigEntry<string> FightingBossCustom { get; }
    public ConfigEntry<string> SingleplayerCustom { get; }
    public ConfigEntry<string> MultiplayerCustom { get; }

    // Race variables (Custom)
    public ConfigEntry<string> RaceImpCustom { get; }
    public ConfigEntry<string> RacePoonCustom { get; }
    public ConfigEntry<string> RaceKuboldCustom { get; }
    public ConfigEntry<string> RaceByrdleCustom { get; }
    public ConfigEntry<string> RaceChangCustom { get; }

    // Class variables (Custom)
    public ConfigEntry<string> ClassNoviceCustom { get; }
    public ConfigEntry<string> ClassFighterCustom { get; }
    public ConfigEntry<string> ClassMysticCustom { get; }
    public ConfigEntry<string> ClassBanditCustom { get; }

    private static string EscapeVars(string input)
    {
        return input.Replace("@", "@0").Replace("{", "@1").Replace("}", "@2");
    }

    private static string EscapeText(string input)
    {
        return input.Replace("@", "@0");
    }

    private static string Unescape(string input)
    {
        return input.Replace("@2", "}").Replace("@1", "{").Replace("@0", "@");
    }

    public string ReplaceVars(string input, GameState state)
    {
        input = EscapeText(input);

        foreach ((var key, var value) in state.GetStates())
        {
            input = input.Replace($"{{{key}}}", EscapeVars(GetVariable(key, value())));
        }

        input = Unescape(input);

        return input;
    }


    public string GetVariable(string variable, string value) => variable switch
    {
        PLAYERCLASS => GetMappedText(value),
        PLAYERRACE => GetMappedText(value),
        PLAYERRACEANDCLASS => string.Join(" ", value.Split(" ").Select(GetMappedText)),
        _ => value
    };

    public string GetMappedText(string str) => str.ToLower() switch
    {
        "imp" => GetText(Texts.RaceImp),
        "poon" => GetText(Texts.RacePoon),
        "kubold" => GetText(Texts.RaceKubold),
        "byrdle" => GetText(Texts.RaceByrdle),
        "chang" => GetText(Texts.RaceChang),
        "novice" => GetText(Texts.ClassNovice),
        "fighter" => GetText(Texts.ClassFighter),
        "mystic" => GetText(Texts.ClassMystic),
        "bandit" => GetText(Texts.ClassBandit),
        _ => str
    };

    public string GetText(Texts text) => DisplayPreset.Value switch
    {
        DisplayPresets.Custom => GetCustomPresetText(text),
        DisplayPresets.Default => GetDefaultPresetText(text),
        DisplayPresets.Emojis => GetEmojisPresetText(text),
        _ => "[Unknown Preset]"
    };

    private string GetCustomPresetText(Texts text) => text switch
    {
        Texts.PlayerAlive => PlayerAliveCustom.Value,
        Texts.PlayerDead => PlayerDeadCustom.Value,
        Texts.MainMenu => MainMenuCustom.Value,
        Texts.Exploring => ExploringCustom.Value,
        Texts.Idle => IdleCustom.Value,
        Texts.FightingInArena => FightingInArenaCustom.Value,
        Texts.FightingBoss => FightingBossCustom.Value,
        Texts.Singleplayer => SingleplayerCustom.Value,
        Texts.Multiplayer => MultiplayerCustom.Value,
        Texts.RaceUnknown => GetDefaultPresetText(text),
        Texts.RaceImp => RaceImpCustom.Value,
        Texts.RacePoon => RacePoonCustom.Value,
        Texts.RaceKubold => RaceKuboldCustom.Value,
        Texts.RaceByrdle => RaceByrdleCustom.Value,
        Texts.RaceChang => RaceChangCustom.Value,
        Texts.ClassUnknown => GetDefaultPresetText(text),
        Texts.ClassNovice => ClassNoviceCustom.Value,
        Texts.ClassFighter => ClassFighterCustom.Value,
        Texts.ClassMystic => ClassMysticCustom.Value,
        Texts.ClassBandit => ClassBanditCustom.Value,
        _ => GetDefaultPresetText(text)
    };

    private string GetDefaultPresetText(Texts text) => text switch
    {
        Texts.PlayerAlive => $"Lv{{{LVL}}} {{{PLAYERRACEANDCLASS}}} ({{{HP}}}/{{{MAXHP}}} HP)",
        Texts.PlayerDead => $"Lv{{{LVL}}} {{{PLAYERRACEANDCLASS}}} (Fainted)",
        Texts.MainMenu => $"In Main Menu",
        Texts.Exploring => $"{{{PLAYERNAME}}} exploring {{{WORLDAREA}}}",
        Texts.Idle => $"{{{PLAYERNAME}}} idle in {{{WORLDAREA}}}",
        Texts.FightingInArena => $"{{{PLAYERNAME}}} fighting in {{{WORLDAREA}}}",
        Texts.FightingBoss => $"{{{PLAYERNAME}}} fighting a boss in {{{WORLDAREA}}}",
        Texts.Singleplayer => $"Singleplayer",
        Texts.Multiplayer => $"Multiplayer on {{{SERVERNAME}}} ({{{PLAYERS}}}/{{{MAXPLAYERS}}})",
        Texts.RaceUnknown => "[Unknown Race]",
        Texts.RaceImp => "Imp",
        Texts.RacePoon => "Poon",
        Texts.RaceKubold => "Kubold",
        Texts.RaceByrdle => "Byrdle",
        Texts.RaceChang => "Chang",
        Texts.ClassUnknown => "[Unknown Class]",
        Texts.ClassNovice => "Novice",
        Texts.ClassFighter => "Fighter",
        Texts.ClassMystic => "Mystic",
        Texts.ClassBandit => "Bandit",
        _ => "[No Text]"
    };

    private string GetEmojisPresetText(Texts text) => text switch
    {
        Texts.PlayerAlive => $"{{{PLAYERRACE}}}{{{PLAYERCLASS}}} 🏆{{{LVL}}} ❤️{{{HP}}} ✨{{{MP}}}⚡{{{SP}}}",
        Texts.PlayerDead => $"{{{PLAYERRACE}}}{{{PLAYERCLASS}}} 🏆{{{LVL}}} 💀",
        Texts.MainMenu => $"📖 Main Menu",
        Texts.Exploring => $"{{{PLAYERNAME}}} 🌎{{{WORLDAREA}}}",
        Texts.Idle => $"{{{PLAYERNAME}}} 🌎{{{WORLDAREA}}} 💤",
        Texts.FightingInArena => $"{{{PLAYERNAME}}} 🌎{{{WORLDAREA}}} ⚔️",
        Texts.FightingBoss => $"{{{PLAYERNAME}}} 🌎{{{WORLDAREA}}} ⚔️👿",
        Texts.Singleplayer => $"👤Solo",
        Texts.Multiplayer => $"👥{{{SERVERNAME}}} ({{{PLAYERS}}}/{{{MAXPLAYERS}}})",
        Texts.RaceUnknown => GetDefaultPresetText(text),
        Texts.RaceImp => "👿",
        Texts.RacePoon => "🐰",
        Texts.RaceKubold => "🐲",
        Texts.RaceByrdle => "🐦",
        Texts.RaceChang => "🐿️",
        Texts.ClassUnknown => GetDefaultPresetText(text),
        Texts.ClassNovice => "🌿",
        Texts.ClassFighter => "🪓", // Axe
        Texts.ClassMystic => "🌀",
        Texts.ClassBandit => "👑",
        _ => GetDefaultPresetText(text)
    };
}
