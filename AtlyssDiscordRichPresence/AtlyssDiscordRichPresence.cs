using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Marioalexsan.AtlyssDiscordRichPresence.SoftDependencies;
using System.Diagnostics;

namespace Marioalexsan.AtlyssDiscordRichPresence;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
[BepInDependency(EasySettings.ModID, BepInDependency.DependencyFlags.SoftDependency)]
public class AtlyssDiscordRichPresence : BaseUnityPlugin
{
    public enum JoinEnableSetting
    {
        JoiningDisabled,
        PublicOnly,
        PublicAndFriends,
        All,
    }

    // The default app hosts the icons and other data used for Rich Presence
    // You can use a custom app ID if you want to customize the icons
    public const string DefaultDiscordAppId = "1309967280842735738";

    public static AtlyssDiscordRichPresence Plugin => _plugin ?? throw new InvalidOperationException($"{nameof(AtlyssDiscordRichPresence)} hasn't been initialized yet. Either wait until initialization, or check via ChainLoader instead.");
    private static AtlyssDiscordRichPresence? _plugin;

    internal new ManualLogSource Logger { get; private set; }

    private readonly Display _display;
    private readonly GameState _state = new();

    private readonly RichPresenceWrapper _richPresence;
    private readonly Harmony _harmony;

    public ConfigEntry<bool> ModEnabled { get; private set; }
    public ConfigEntry<JoinEnableSetting> ServerJoinSetting { get; private set; }
    public ConfigEntry<string> DiscordAppId { get; private set; }
    public ConfigEntry<RichPresenceWrapper.LogLevels> DiscordRPCLogLevel { get; private set; }

    public AtlyssDiscordRichPresence()
    {
        _plugin = this;
        Logger = base.Logger;
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);

        _display = new(Config);
        ModEnabled = Config.Bind("General", "Enable", true, "Enable or disable this mod. While disabled, no in-game data is shown.");
        ServerJoinSetting = Config.Bind("General", "ServerJoinSetting", JoinEnableSetting.PublicAndFriends, "Set the server privacy levels for which Discord should allow joining.");
        DiscordRPCLogLevel = Config.Bind("General", "DiscordRPCLogLevel", RichPresenceWrapper.LogLevels.Warning, "Log level to use for logs coming from the Discord RPC library.");
        DiscordAppId = Config.Bind("Advanced", "DiscordAppId", DefaultDiscordAppId, "The Discord application ID to be used by the mod. ***Do not change this unless you know what you're doing!***");

        _richPresence = new(DiscordAppId.Value, Logger, DiscordRPCLogLevel.Value);
        _richPresence.OnJoin += OnJoinLobby;
    }

    private void OnJoinLobby(object sender, RichPresenceWrapper.JoinData e)
    {
        Logging.LogInfo($"OnJoinLobby " + e.Id);

        string actualId = e.Id;

        if (actualId.StartsWith("secret_"))
            actualId = actualId["secret_".Length..];

        if (!ulong.TryParse(actualId, out ulong steamId))
        {
            Logging.LogInfo($"ID is invalid, can't join.");
            return;
        }

        SteamLobby._current.Init_LobbyJoinRequest(new Steamworks.CSteamID(steamId));
    }

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
            _state.ServerJoinId = SteamLobby._current._currentLobbyID.ToString();
        }
        else
        {
            _state.InMultiplayer = false;
            _state.Players = 1;
            _state.MaxPlayers = 1;
            _state.ServerJoinId = "";
        }

        _richPresence.Tick();
    }

    private void Awake()
    {
        InitializeConfiguration();
        _harmony.PatchAll();

        _richPresence.Enabled = ModEnabled.Value;
    }

    private void InitializeConfiguration()
    {
        if (EasySettings.IsAvailable)
        {
            EasySettings.OnApplySettings.AddListener(() =>
            {
                try
                {
                    Config.Save();

                    _richPresence.LogLevel = DiscordRPCLogLevel.Value;
                    _richPresence.Enabled = ModEnabled.Value;
                }
                catch (Exception e)
                {
                    Logging.LogError($"AtlyssDiscordRichPresence crashed in OnApplySettings! Please report this error to the mod developer:");
                    Logging.LogError(e.ToString());
                }
            });
            EasySettings.OnInitialized.AddListener(() =>
            {
                // DiscordAppId is not included on purpose 
                EasySettings.AddHeader(ModInfo.PLUGIN_NAME);
                EasySettings.AddToggle("Enabled", ModEnabled);
                EasySettings.AddDropdown("Enable server joining in Discord", ServerJoinSetting);
                EasySettings.AddDropdown("DiscordRPC log level", DiscordRPCLogLevel);
                EasySettings.AddDropdown("Rich Presence display preset", _display.DisplayPreset);
                // TODO: Text inputs on EasySettings aren't supported (yet?), so we can't configure custom presets here
            });
        }
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
            Details = _display.ReplaceVars(_display.GetText(Display.Texts.MainMenu), _state),
            LargeImageKey = Assets.ATLYSS_ICON,
            LargeImageText = "ATLYSS"
        }, TimerTrackerState.MainMenu);
    }

    private void UpdateWorldAreaPresence(Player? player, MapInstance? area)
    {
        if (player != null)
            _state.UpdateData(player);

        if (area != null)
            _state.UpdateData(area);

        var details = _display.ReplaceVars(_display.GetText(Display.Texts.Exploring), _state);

        if (_state.IsIdle)
            details = _display.ReplaceVars(_display.GetText(Display.Texts.Idle), _state);

        if (_state.InArenaCombat)
            details = _display.ReplaceVars(_display.GetText(Display.Texts.FightingInArena), _state);

        // TODO: This doesn't update correctly in some cases, need to check whenever pattern manager is active in Update()?
        if (_state.InBossCombat)
            details = _display.ReplaceVars(_display.GetText(Display.Texts.FightingBoss), _state);

        var state = _display.ReplaceVars(_display.GetText(Display.Texts.PlayerAlive), _state);

        if (_state.HealthPercentage <= 0)
            state = _display.ReplaceVars(_display.GetText(Display.Texts.PlayerDead), _state);


        UpdatePresence(new()
        {
            Details = details,
            State = state,
            LargeImageKey = !_state.InMultiplayer ? Assets.ATLYSS_SINGLEPLAYER : Assets.ATLYSS_MULTIPLAYER,
            LargeImageText = !_state.InMultiplayer ? _display.ReplaceVars(_display.GetText(Display.Texts.Singleplayer), _state) : _display.ReplaceVars(_display.GetText(Display.Texts.Multiplayer), _state),
            SmallImageKey = MapAreaToIcon(_state.WorldArea),
            SmallImageText = _state.WorldArea,
            Multiplayer = !_state.InMultiplayer ? null : new ServerData()
            {
                Id = _state.ServerJoinId,
                Size = _state.Players,
                Max = _state.MaxPlayers,
                AllowJoining = CanJoinMultiplayer()
            }
        }, TimerTrackerState.ExploringWorld);
    }

    private bool CanJoinMultiplayer()
    {
        // 0 - Public / 1 - Friends Only / 2 - Private
        var lobbyType = LobbyListManager._current._lobbyTypeDropdown.value;

        return lobbyType switch
        {
            0 => ServerJoinSetting.Value >= JoinEnableSetting.PublicOnly,
            1 => ServerJoinSetting.Value >= JoinEnableSetting.PublicAndFriends,
            2 => ServerJoinSetting.Value >= JoinEnableSetting.All,
            _ => false
        };
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