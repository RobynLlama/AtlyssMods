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

namespace Marioalexsan.AtlyssDiscordRichPresence;

[BepInPlugin("Marioalexsan.AtlyssDiscordRPC", "Discord Rich Presence for Atlyss", "1.0.0")]
public class AtlyssDiscordRichPresenceMod : BaseUnityPlugin
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
    private readonly TimeSpan _rateLimit = TimeSpan.FromSeconds(1);

    private RichPresence _currentPresence = new();
    private DateTime _presenceLastSent;
    private bool _presenceNeedsUpdate = false;
    private bool _presenceIgnoreRateLimit = false;

    // Doesn't really update
    private readonly RichPresence _mainMenuPresence = new RichPresence()
    {
        Details = "In Main Menu",
        Assets = new Assets()
        {
            LargeImageKey = "atlyss_icon",
            LargeImageText = "ATLYSS"
        }
    };

    // Updated dynamically
    private readonly RichPresence _worldAreaPresence = new()
    {
        Assets = new Assets()
    };

    private void Update()
    {
        var now = DateTime.Now;
        
        if (_presenceState == PresenceState.ExploringWorld)
        {
            UpdateWorldAreaPresence(Player._mainPlayer, null);
        }

        // Discord's rate limit seems to be one update every 15 seconds, so let's try to buffer updates until then
        // It might be possible to send more messages as a burst, though
        if (_presenceNeedsUpdate && (_presenceIgnoreRateLimit || _presenceLastSent + _rateLimit <= now))
        {
            _presenceLastSent = now;
            _presenceNeedsUpdate = false;
            _presenceIgnoreRateLimit = false;
            Logger.LogInfo("Trying to send Rich Presence update...");
            _client.SetPresence(_currentPresence);
        }
    }

    private void Awake()
    {
        _timeStart = DateTime.UtcNow;
        _presenceLastSent = DateTime.UtcNow;

        _client = new DiscordRpcClient(DiscordAppId, pipe: discordPipe)
        {
            Logger = new DiscordRPC.Logging.ConsoleLogger(_logLevel, true)
        };

        _client.Logger = new ConsoleLogger() { Level = DiscordRPC.Logging.LogLevel.Warning };

        _client.OnReady += (sender, e) =>
        {
            Console.WriteLine("Received Ready from user {0}", e.User.Username);

            _presenceLastSent = DateTime.UtcNow;
            _currentPresence = _mainMenuPresence;
            _client.SetPresence(_currentPresence);
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

    private void OnDestroy()
    {
        _client.Dispose();
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
            _lastCombatState = false;
            _lastCombatStateIsBoss = false;
            UpdatePresenceState(PresenceState.ExploringWorld);
            UpdateWorldAreaPresence(self, _new);
        }
    }

    private void SetPresence(RichPresence presence, bool sendNow = false)
    {
        presence.Timestamps = new Timestamps()
        {
            Start = _timeStart
        };

        _currentPresence = presence;
        _presenceNeedsUpdate = true;
        _presenceIgnoreRateLimit = sendNow;
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
        SetPresence(_mainMenuPresence);
    }

    private string _lastWorldArea = "";
    private string _lastPlayerName = "";
    private int _lastLevel = 0;
    private int _lastHealth = 0;
    private int _lastMaxHealth = 0;
    private int _lastMana = 0;
    private int _lastMaxMana = 0;
    private int _lastStamina = 0;
    private int _lastMaxStamina = 0;
    private bool _lastCombatState = false;
    private bool _lastCombatStateIsBoss = false;

    private void UpdateWorldAreaPresence(Player player, MapInstance area)
    {
        var worldArea = _lastWorldArea = area?._mapName ?? _lastWorldArea;

        var name = _lastPlayerName = player?._nickname ?? _lastPlayerName;
        var level = _lastLevel = player?._statusEntity._pStats._currentLevel ?? _lastLevel;

        var health = _lastHealth = player?._statusEntity._currentHealth ?? _lastHealth;
        var maxHealth = _lastMaxHealth = player?._statusEntity._pStats.Network_statStruct._maxHealth ?? _lastMaxHealth;

        var mana = _lastMana = player?._statusEntity._currentMana ?? _lastMana;
        var maxMana = _lastMaxMana = player?._statusEntity._pStats.Network_statStruct._maxMana ?? _lastMaxMana;

        var stamina = _lastStamina = player?._statusEntity._currentStamina ?? _lastStamina;
        var maxStamina = _lastMaxStamina = player?._statusEntity._pStats.Network_statStruct._maxStamina ?? _lastMaxStamina;

        var combatState = _lastCombatState;
        var combatIsBoss = _lastCombatStateIsBoss;

        var healthPct = maxHealth == 0 ? 0 : Math.Clamp(health * 100 / maxHealth, 0, 100);
        var manaPct = maxMana == 0 ? 0 : Math.Clamp(mana * 100 / maxMana, 0, 100);
        var staminaPct = maxStamina == 0 ? 0 : Math.Clamp(stamina * 100 / maxStamina, 0, 100);

        bool displayActual = true;

        var action = "Exploring";

        if (combatState)
            action = "Fighting in";

        // TODO: This doesn't update correctly in some cases, need to check whenever pattern manager is active in Update()?
        if (combatState && combatIsBoss)
            action = "Fighting a boss in";

        var state = $"{name} Lv{level} ({healthPct}% HP {manaPct}% MP)";

        if (displayActual)
            state = $"{name} Lv{level} ({health}/{maxHealth} HP {mana}/{maxMana} MP)";

        if (healthPct == 0)
            state = $"{name} Lv{level} (Fainted)";

        bool needsUpdate = false;

        if (_worldAreaPresence.Details != $"{action} {worldArea}")
        {
            _worldAreaPresence.Details = $"{action} {worldArea}";
            needsUpdate = true;
        }

        if (_worldAreaPresence.State != state)
        {
            _worldAreaPresence.State = state;
            needsUpdate = true;
        }

        if (_worldAreaPresence.Assets.LargeImageKey != "atlyss_icon")
        {
            _worldAreaPresence.Assets.LargeImageKey = "atlyss_icon";
            needsUpdate = true;
        }

        if (_worldAreaPresence.Assets.LargeImageText != "ATLYSS")
        {
            _worldAreaPresence.Assets.LargeImageText = "ATLYSS";
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            // Set presence
            // If world area changed, we probably want to update that right away
            SetPresence(_worldAreaPresence, area != null);
        }
    }
}