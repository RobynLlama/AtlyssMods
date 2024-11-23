using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using DiscordRPC.Logging;
using DiscordRPC;
using UnityEngine;
using BepInEx.Logging;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace AtlyssDiscordRPC;

[BepInPlugin("Marioalexsan.AtlyssDiscordRPC", "Discord Rich Presence for Atlyss", "1.0.0")]
public class AtlyssDiscordRPC : BaseUnityPlugin
{
    private enum PresenceState
    {
        MainMenu,
        ExploringWorld
    }

    private PresenceState _presenceState = PresenceState.MainMenu;

    private static readonly string DiscordAppId = "1309967280842735738";
    private DiscordRpcClient _client;
    private DateTime _timeStart;

    private static DiscordRPC.Logging.LogLevel _logLevel = DiscordRPC.Logging.LogLevel.Trace;
    private static int discordPipe = -1;

    private DateTime _lastUpdate;

    private void Update()
    {
        var now = DateTime.Now;

        if (_lastUpdate + TimeSpan.FromSeconds(2) < now)
        {
            _lastUpdate = now;

            if (_presenceState == PresenceState.ExploringWorld)
            {
                UpdateWorldAreaPresence(Player._mainPlayer, null);
            }
        }
    }

    private void Awake()
    {
        _timeStart = DateTime.UtcNow;
        _client = new DiscordRpcClient(DiscordAppId, pipe: discordPipe)
        {
            Logger = new DiscordRPC.Logging.ConsoleLogger(_logLevel, true)
        };

        _client.Logger = new ConsoleLogger() { Level = DiscordRPC.Logging.LogLevel.Warning };

        _client.OnReady += (sender, e) =>
        {
            Console.WriteLine("Received Ready from user {0}", e.User.Username);
        };

        _client.OnPresenceUpdate += (sender, e) =>
        {
            Console.WriteLine("Received Update! {0}", e.Presence);
        };

        _client.Initialize();

        On.Player.OnPlayerMapInstanceChange += Player_OnPlayerMapInstanceChange;
        On.MainMenuManager.Set_MenuCondition += MainMenuManager_Set_MenuCondition;
        On.PatternInstanceManager.Update += PatternInstanceManager_Update;
    }

    private void PatternInstanceManager_Update(On.PatternInstanceManager.orig_Update orig, PatternInstanceManager self)
    {
        orig(self);

        bool isBoss = (bool)self._muBossSrc && self._muBossSrc.isPlaying && self._muBossSrc.volume > 0.1f;
        bool isAction = (bool)self._muActionSrc && self._muActionSrc.isPlaying && self._muActionSrc.volume > 0.1f;
        bool inCombat = isBoss || isAction;

        // Update action state if combat state changed
        if (inCombat != _lastCombatState)
        {
            _lastCombatState = inCombat;
            _lastCombatStateIsBoss = isBoss;
            Logger.LogInfo("Detected combat state change!");
            UpdateWorldAreaPresence(null, null);
        }
    }

    private void OnDestroy()
    {
        _client.Dispose();
    }

    private void MainMenuManager_Set_MenuCondition(On.MainMenuManager.orig_Set_MenuCondition orig, MainMenuManager self, int _index)
    {
        orig(self, _index);
        UpdatePresenceState(PresenceState.MainMenu);
        UpdateMainMenuPresence(self);
    }

    private void Player_OnPlayerMapInstanceChange(On.Player.orig_OnPlayerMapInstanceChange orig, Player self, MapInstance _old, MapInstance _new)
    {
        orig(self, _old, _new);

        if (self == Player._mainPlayer)
        {
            UpdatePresenceState(PresenceState.ExploringWorld);
            UpdateWorldAreaPresence(self, _new);
        }
    }

    private void SetPresence(RichPresence presence)
    {
        presence.Timestamps = new Timestamps()
        {
            Start = _timeStart
        };
        _client.SetPresence(presence);
    }

    private void UpdatePresenceState(PresenceState state, bool resetTime = false)
    {
        if (_presenceState != state || resetTime)
        {
            _timeStart = DateTime.UtcNow;
            _presenceState = state;
        }
    }

    private void UpdateMainMenuPresence(MainMenuManager manager)
    {
        SetPresence(new RichPresence()
        {
            Details = "In Main Menu",
            Assets = new Assets()
            {
                LargeImageKey = "atlyss_icon",
                LargeImageText = "ATLYSS"
            }
        });
    }

    private string _lastWorldArea = "";
    private string _lastPlayerName = "";
    private int _lastHealth = 0;
    private int _lastMaxHealth = 0;
    private int _lastMana = 0;
    private int _lastMaxMana = 0;
    private bool _lastCombatState = false;
    private bool _lastCombatStateIsBoss = false;

    private void UpdateWorldAreaPresence(Player player, MapInstance area)
    {
        var worldArea = _lastWorldArea = area?._mapName ?? _lastWorldArea;

        var name = _lastPlayerName = player?._nickname ?? _lastPlayerName;

        var health = _lastHealth = player?._statusEntity._currentHealth ?? _lastHealth;
        var maxHealth = _lastMaxHealth = player?._statusEntity._pStats._baseStatStruct._maxHealth ?? _lastMaxHealth;

        var mana = _lastMana = player?._statusEntity._currentMana ?? _lastMana;
        var maxMana = _lastMaxMana = player?._statusEntity._pStats._baseStatStruct._maxMana ?? _lastMaxMana;

        var combatState = _lastCombatState;
        var combatIsBoss = _lastCombatStateIsBoss;

        var healthPct = maxHealth == 0 ? 0 : Math.Clamp(health * 100 / maxHealth, 0, 100);
        var manaPct = maxMana == 0 ? 0 : Math.Clamp(mana * 100 / maxMana, 0, 100);

        SetPresence(new RichPresence()
        {
            Details = $"{(combatState ? combatIsBoss ? "Fighting boss in" : "Fighting creeps in" : "Exploring")} {worldArea}",
            State = $"{name} ({healthPct}% HP, {manaPct}% MP)",
            Assets = new Assets()
            {
                LargeImageKey = "atlyss_icon",
                LargeImageText = "ATLYSS"
            }
        });
    }
}