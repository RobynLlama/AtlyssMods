using System;
using System.Collections.Generic;
using System.Text;

namespace Marioalexsan.AtlyssDiscordRichPresence;

// TODO: Player race
// TODO: Player class

public class GameState
{
    public string ReplaceVars(string input)
    {
        foreach (var kvp in _keys)
        {
            input = input.Replace($"{{{kvp.Key}}}", kvp.Value());
        }

        return input;
    }

    private readonly Dictionary<string, Func<string>> _keys;

    public GameState()
    {
        _keys = new()
        {
            [States.HP] = () => $"{Health}",
            [States.MAXHP] = () => $"{MaxHealth}",
            [States.HPPCT] = () => $"{HealthPercentage}",

            [States.MP] = () => $"{Mana}",
            [States.MAXMP] = () => $"{MaxMana}",
            [States.MPPCT] = () => $"{ManaPercentage}",

            [States.SP] = () => $"{Stamina}",
            [States.MAXSP] = () => $"{MaxStamina}",
            [States.SPPCT] = () => $"{StaminaPercentage}",

            [States.LVL] = () => $"{Level}",

            [States.EXP] = () => $"{Experience}",
            [States.EXPNEXT] = () => $"{ExperienceForNextLevel}",
            [States.EXPPCT] = () => $"{ExperiencePercentage}",

            [States.PLAYERNAME] = () => $"{PlayerName}",
            [States.PLAYERRACE] = () => $"{PlayerRace}",
            [States.PLAYERCLASS] = () => $"{PlayerClass}",
            [States.PLAYERRACEANDCLASS] = () => $"{PlayerRace} {PlayerClass}".Trim(),

            [States.WORLDAREA] = () => $"{WorldArea}",

            [States.SERVERNAME] = () => $"{ServerName}",
            [States.PLAYERS] = () => $"{Players}",
            [States.MAXPLAYERS] = () => $"{MaxPlayers}"
        };
    }

    // Displayable

    public int Health { get; set; }
    public int Mana { get; set; }
    public int Stamina { get; set; }

    public int MaxHealth { get; set; }
    public int MaxMana { get; set; }
    public int MaxStamina { get; set; }

    public int HealthPercentage => MaxHealth != 0 ? Health * 100 / MaxHealth : 0;
    public int ManaPercentage => MaxMana != 0 ? Mana * 100 / MaxMana : 0;
    public int StaminaPercentage => MaxStamina != 0 ? Stamina * 100 / MaxStamina : 0;

    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceForNextLevel { get; set; }
    public int ExperiencePercentage => ExperienceForNextLevel != 0 ? Experience * 100 / ExperienceForNextLevel : 0;

    public string PlayerName { get; set; }
    public string PlayerRace { get; set; }
    public string PlayerClass { get; set; }

    public string WorldArea { get; set; }

    // Not configurable / displayable

    public bool InArenaCombat { get; set; }
    public bool InBossCombat { get; set; }

    public bool InMultiplayer { get; set; }
    public int Players { get; set; }
    public int MaxPlayers { get; set; }
    public string ServerName { get; set; }

    public void UpdateData(MapInstance area)
    {
        if (area == null)
            return;

        WorldArea = area._mapName;
    }

    public void UpdateData(Player player)
    {
        if (player == null)
            return;

        Health = player._statusEntity.Network_currentHealth;
        Mana = player._statusEntity.Network_currentMana;
        Stamina = player._statusEntity.Network_currentStamina;

        MaxHealth = player._statusEntity._pStats.Network_statStruct._maxHealth;
        MaxMana = player._statusEntity._pStats.Network_statStruct._maxMana;
        MaxStamina = player._statusEntity._pStats.Network_statStruct._maxStamina;

        Level = player._statusEntity._pStats.Network_currentLevel;
        Experience = player._statusEntity._pStats.Network_currentExp;
        ExperienceForNextLevel = player._statusEntity._pStats.Network_statStruct._experience;

        PlayerName = player.Network_nickname;
        PlayerRace = player._pVisual._playerAppearanceStruct._setRaceTag ?? "";

        if ((bool)player._pStats._class)
        {
            PlayerClass = player._pStats._class._className ?? "";
        }
        else
        {
            PlayerClass = GameManager._current._statLogics._emptyClassName ?? "";
        }
    }
}
