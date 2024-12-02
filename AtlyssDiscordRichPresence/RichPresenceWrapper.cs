using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions.Must;

namespace Marioalexsan.AtlyssDiscordRichPresence;
public class RichPresenceWrapper : IDisposable
{
    private readonly DiscordRpcClient _client;
    private DateTime _lastUpdate;
    private PresenceData _presenceData;
    private bool _shouldSendPresence = false;
    private readonly RichPresence _presence = new()
    {
        Assets = new(),
        Party = new(),
        Timestamps = new()
    };

    public TimeSpan RateLimit { get; set; } = TimeSpan.FromSeconds(1);

    public bool Enabled
    {
        get => _client.IsInitialized;
        set
        {
            if (_client.IsInitialized && !value)
                _client.Deinitialize();
            else if (!_client.IsInitialized && value)
                _client.Initialize();
        }
    }

    public RichPresenceWrapper(string discordAppId, LogLevel logLevel, bool startEnabled = true)
    {
        _lastUpdate = DateTime.UtcNow;

        _client = new DiscordRpcClient(discordAppId)
        {
            Logger = new ConsoleLogger(logLevel, true)
        };

        _client.OnReady += (sender, e) =>
        {
            Console.WriteLine($"DiscordRpcClient initialized for user {e.User.Username}");

            _lastUpdate = DateTime.UtcNow;
            _presence.Timestamps.Start = DateTime.UtcNow;
            SetPresence(_presenceData);
        };

        _client.OnClose += (sender, e) =>
        {
            Console.WriteLine($"DiscordRpcClient deinitialized.");
        };

        if (startEnabled)
        {
            _client.Initialize();
        }
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

        _presenceData = data;
        _shouldSendPresence = true;
    }

    public void Tick()
    {
        if (_shouldSendPresence && _lastUpdate + RateLimit <= DateTime.UtcNow)
        {
            _lastUpdate = DateTime.UtcNow;
            _shouldSendPresence = false;
            _client.SetPresence(_presence);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
