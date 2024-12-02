using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using DiscordRPC.Logging;
using DiscordRPC;
using UnityEngine;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.AtlyssDiscordRichPresence;

public class ModAwareMultiplayerVanillaCompatibleAttribute : Attribute { }

[BepInPlugin("Marioalexsan.AtlyssDiscordRichPresence", "AtlyssDiscordRichPresence", "1.1.0")]
[ModAwareMultiplayerVanillaCompatible]
public class AtlyssDiscordRichPresenceMod : BaseUnityPlugin
{
    public static AtlyssDiscordRichPresenceMod Instance { get; private set; }

    private const string DiscordAppId = "1309967280842735738";

    private readonly Display _display = new();
    private readonly GameState _state = new();

    private RichPresenceWrapper _richPresence;
    private Harmony _harmony;

    public bool ModEnabled { get; private set; } = true;

    private enum TimerTrackerState
    {
        MainMenu,
        ExploringWorld
    }

    private TimerTrackerState _timeTrackerState;

    private void UpdatePresence(PresenceData data, TimerTrackerState state)
    {
        _richPresence.SetPresence(data, _timeTrackerState != state);
        _timeTrackerState = state;
    }

    private void Update()
    {
        if (_timeTrackerState == TimerTrackerState.ExploringWorld)
        {
            UpdateWorldAreaPresence(Player._mainPlayer, null);
        }

        if (
            (bool)AtlyssNetworkManager._current &&
            !AtlyssNetworkManager._current._soloMode && 
            !(AtlyssNetworkManager._current._steamworksMode && !SteamManager.Initialized) &&
            (bool)Player._mainPlayer
            )
        {
            _state.InMultiplayer = true;
            _state.Players = FindObjectsOfType<Player>().Length;
            _state.MaxPlayers = ServerInfoObject._current._maxConnections;
            _state.ServerName = ServerInfoObject._current._serverName;
        }
        else
        {
            _state.InMultiplayer = false;
            _state.Players = 1;
            _state.MaxPlayers = 1;
        }

        _richPresence.Tick();
    }

    private void Awake()
    {
        InitializeConfiguration();

        _harmony = new Harmony("Marioalexsan.AtlyssDiscordRichPresence");
        _harmony.PatchAll();

        Instance = this;
        _richPresence = new(DiscordAppId, DiscordRPC.Logging.LogLevel.Warning, ModEnabled);
    }

    private void InitializeConfiguration()
    {
        _display.PlayerAlive = Config.Bind(nameof(Display), nameof(Display.PlayerAlive), _display.PlayerAlive, Display.PlayerAliveNote).Value;
        _display.PlayerDead = Config.Bind(nameof(Display), nameof(Display.PlayerDead), _display.PlayerDead, Display.PlayerDeadNote).Value;
        _display.MainMenu = Config.Bind(nameof(Display), nameof(Display.MainMenu), _display.MainMenu, Display.MainMenuNote).Value;
        _display.Exploring = Config.Bind(nameof(Display), nameof(Display.Exploring), _display.Exploring, Display.ExploringNote).Value;
        _display.Idle = Config.Bind(nameof(Display), nameof(Display.Idle), _display.Idle, Display.IdleNote).Value;
        _display.FightingInArena = Config.Bind(nameof(Display), nameof(Display.FightingInArena), _display.FightingInArena, Display.FightingInArenaNote).Value;
        _display.FightingBoss = Config.Bind(nameof(Display), nameof(Display.FightingBoss), _display.FightingBoss, Display.FightingBossNote).Value;
        _display.Singleplayer = Config.Bind(nameof(Display), nameof(Display.Singleplayer), _display.Singleplayer, Display.SingleplayerNote).Value;
        _display.Multiplayer = Config.Bind(nameof(Display), nameof(Display.Multiplayer), _display.Multiplayer, Display.MultiplayerNote).Value;

        ModEnabled = Config.Bind("General", "Enable", true, "Enable or disable this mod. While disabled, no in-game data is shown.").Value;
    }

    private void OnDestroy()
    {
        _richPresence.Dispose();
    }

    internal void PatternInstanceManager_Update(PatternInstanceManager self)
    {
        // TODO: Find a better way to get this than music state?
        bool isBoss = (bool)self._muBossSrc && self._muBossSrc.isPlaying && self._muBossSrc.volume > 0.1f;
        bool isAction = (bool)self._muActionSrc && self._muActionSrc.isPlaying && self._muActionSrc.volume > 0.1f;

        _state.InArenaCombat = isAction;
        _state.InBossCombat = isBoss;
        UpdateWorldAreaPresence(null, null);
    }

    internal void MainMenuManager_Set_MenuCondition(MainMenuManager self)
    {
        UpdateMainMenuPresence(self);
    }

    internal void Player_OnPlayerMapInstanceChange(Player self, MapInstance _new)
    {
        if (self == Player._mainPlayer)
        {
            _state.InArenaCombat = false;
            _state.InBossCombat = false;
            UpdateWorldAreaPresence(self, _new);
        }
    }

    private void UpdateMainMenuPresence(MainMenuManager manager)
    {
        UpdatePresence(new()
        {
            Details = "In Main Menu",
            LargeImageKey = "atlyss_icon",
            LargeImageText = "ATLYSS"
        }, TimerTrackerState.MainMenu);
    }

    private void UpdateWorldAreaPresence(Player player, MapInstance area)
    {
        _state.UpdateData(player);
        _state.UpdateData(area);

        var details = _state.ReplaceVars(_display.Exploring);

        if (_state.InArenaCombat)
            details = _state.ReplaceVars(_display.FightingInArena);

        // TODO: This doesn't update correctly in some cases, need to check whenever pattern manager is active in Update()?
        if (_state.InBossCombat)
            details = _state.ReplaceVars(_display.FightingBoss);

        var state = _state.ReplaceVars(_display.PlayerAlive);

        if (_state.HealthPercentage <= 0)
            state = _state.ReplaceVars(_display.PlayerDead);

        UpdatePresence(new()
        {
            Details = details,
            State = state,
            LargeImageKey = !_state.InMultiplayer ? Assets.ATLYSS_SINGLEPLAYER : Assets.ATLYSS_MULTIPLAYER,
            LargeImageText = !_state.InMultiplayer ? _state.ReplaceVars(_display.Singleplayer) : _state.ReplaceVars(_display.Multiplayer),
            SmallImageKey = MapAreaToIcon(_state.WorldArea),
            SmallImageText = _state.WorldArea
        }, TimerTrackerState.ExploringWorld);
    }

    // This doesn't follow the World Portal icons because uhhhhhh reasons
    private static string MapAreaToIcon(string area) => area.ToLower() switch
    {
        // map_dungeon00_sanctumCatacoms
        "sanctum catacombs" => Assets.ZONESELECTIONICON_DUNGEON,
        // map_dungeon00_crescentGrove
        "crescent grove" => Assets.ZONESELECTIONICON_DUNGEON,

        // map_pvp_sanctumArena
        "sanctum arena" => Assets.ZONESELECTIONICON_ARENA,
        // map_pvp_catacombsArena
        "executioner's tomb" => Assets.ZONESELECTIONICON_ARENA,

        // map_hub_sanctum
        "sanctum" => Assets.ZONESELECTIONICON_SAFE,
        // map_hub_wallOfTheStars
        "wall of the stars" => Assets.ZONESELECTIONICON_SAFE,

        // map_zone00_outerSanctumGate
        "outer sanctum gate" => Assets.ZONESELECTIONICON_FIELD,
        // map_zone00_outerSanctum
        "outer sanctum" => Assets.ZONESELECTIONICON_FIELD,
        // map_zone00_effoldTerrace
        "effold terrace" => Assets.ZONESELECTIONICON_FIELD,
        // map_zone00_tuulValley
        "tuul valley" => Assets.ZONESELECTIONICON_FIELD,
        // map_zone00_woodreach
        "woodreach pass" => Assets.ZONESELECTIONICON_FIELD,
        // map_zone00_crescentKeep
        "crescent keep" => Assets.ZONESELECTIONICON_FIELD,
        // map_zone00_gateOfTheMoon
        "gate of the moon" => Assets.ZONESELECTIONICON_FIELD,

        _ => Assets.ZONESELECTIONICON_FIELD
    };
}