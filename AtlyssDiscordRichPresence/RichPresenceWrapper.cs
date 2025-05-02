using BepInEx;
using BepInEx.Logging;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Marioalexsan.AtlyssDiscordRichPresence;
public class RichPresenceWrapper : IDisposable
{
    private const string AtlyssSteamAppId = "2768430";

    // TODO: EasySettings has a bug where enums in dropdowns aren't mapped correctly to their underlying values
    // This means that we need an enum that has sequential values starting from 0, and that we need to map it to the DiscordRPC's LogLevel
    public enum LogLevels
    {
        Trace,
        Info,
        Warning,
        Error,
        None
    }

    public struct JoinData
    {
        public string Id;
    }

    private readonly DiscordRpcClient _client;
    private readonly DiscordLogSourceLogger _logger;

    private readonly RichPresence _presence = new()
    {
        Assets = new(),
        Party = new(),
        Timestamps = new(),
        Secrets = new()
    };

    private DateTime _lastUpdate;
    private PresenceData _presenceData;
    private bool _shouldSendPresence = false;

    // Rate limit on our side in case values for stuff like HP, SP, etc. change rapidly
    // This makes updates slow, but consistent (otherwise they would be fast but then get rate limited by Discord)
    public TimeSpan RateLimit { get; set; } = TimeSpan.FromSeconds(3);

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                Logging.LogInfo($"Presence sending is now {(value ? "enabled" : "disabled")}.");
            }

            _enabled = value;
            _shouldSendPresence = true;
        }
    }
    private bool _enabled;

    public event EventHandler<JoinData>? OnAskToJoin;
    public event EventHandler<JoinData>? OnJoin;

    public LogLevels LogLevel
    {
        get => _logger.Level switch
        {
            DiscordRPC.Logging.LogLevel.Trace => LogLevels.Trace,
            DiscordRPC.Logging.LogLevel.Info => LogLevels.Info,
            DiscordRPC.Logging.LogLevel.Warning => LogLevels.Warning,
            DiscordRPC.Logging.LogLevel.Error => LogLevels.Error,
            DiscordRPC.Logging.LogLevel.None => LogLevels.None,
            _ => LogLevels.Info,
        };
        set
        {
            var mappedValue = value switch
            {
                LogLevels.Trace => DiscordRPC.Logging.LogLevel.Trace,
                LogLevels.Info => DiscordRPC.Logging.LogLevel.Info,
                LogLevels.Warning => DiscordRPC.Logging.LogLevel.Warning,
                LogLevels.Error => DiscordRPC.Logging.LogLevel.Error,
                LogLevels.None => DiscordRPC.Logging.LogLevel.None,
                _ => DiscordRPC.Logging.LogLevel.Info,
            };

            if (_logger.Level != mappedValue)
            {
                Logging.LogInfo($"DiscordRPC log level has been set to {mappedValue}.");
            }

            _logger.Level = mappedValue;
        }
    }

    public RichPresenceWrapper(string discordAppId, ManualLogSource logSource, LogLevels logLevel)
    {
        _logger = new DiscordLogSourceLogger(logSource, DiscordRPC.Logging.LogLevel.None);
        LogLevel = logLevel;

        _lastUpdate = DateTime.UtcNow;

        _client = new DiscordRpcClient(discordAppId, -1, _logger, false, null);

        _client.OnReady += (sender, e) =>
        {
            _logger.Info($"DiscordRpcClient initialized for user {e.User.Username}.");

            _lastUpdate = DateTime.UtcNow;
            _presence.Timestamps.Start = DateTime.UtcNow;
            SetPresence(_presenceData);
        };

        _client.OnClose += (sender, e) =>
        {
            _logger.Info($"DiscordRpcClient deinitialized.");
        };

        _client.OnJoin += (sender, e) =>
        {
            _logger.Info($"Got a join event.");
            OnJoin?.Invoke(sender, new()
            {
                Id = e.Secret
            });
        };

        _client.OnJoinRequested += (sender, e) =>
        {
            _logger.Info($"Got a join request event.");
        };

        _client.RegisterUriScheme(steamAppID: AtlyssSteamAppId);
        _client.SetSubscription(EventType.Join | EventType.JoinRequest);
        _client.Initialize();
    }

    public void SetPresence(PresenceData data, bool resetTimer = false)
    {
        _presence.State = data.State;
        _presence.Details = data.Details;
        _presence.Assets.LargeImageKey = data.LargeImageKey;
        _presence.Assets.LargeImageText = data.LargeImageText;
        _presence.Assets.SmallImageKey = data.SmallImageKey;
        _presence.Assets.SmallImageText = data.SmallImageText;

        if (resetTimer)
        {
            _presence.Timestamps.Start = DateTime.UtcNow;
        }

        if (data.Multiplayer.HasValue)
        {
            var lobbyId = data.Multiplayer.Value.Id;

            using var sha256 = SHA256.Create();
            var hashedLobbyId = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF32.GetBytes(lobbyId))).Replace("-", "");

            // Party ID and Join Secret must be at least 2 characters long according to Discord

            _presence.Party.ID = $"party_{hashedLobbyId}";
            _presence.Party.Size = data.Multiplayer.Value.Size;
            _presence.Party.Max = data.Multiplayer.Value.Max;
            _presence.Party.Privacy = Party.PrivacySetting.Public;
            _presence.Secrets.JoinSecret = data.Multiplayer.Value.AllowJoining ? $"secret_{lobbyId}" : "";
        }
        else
        {
            _presence.Party.ID = "";
            _presence.Secrets.JoinSecret = "";
        }

        _presenceData = data;
        _shouldSendPresence = true;
    }

    public void Tick()
    {
        if (_shouldSendPresence && _lastUpdate + RateLimit <= DateTime.UtcNow)
        {
            _lastUpdate = DateTime.UtcNow;
            _shouldSendPresence = false;

            // Sending null clears the current presence
            _client.SetPresence(Enabled ? _presence : null);
        }

        if (!_client.AutoEvents)
            _client.Invoke();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
