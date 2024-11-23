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
    private static readonly string DiscordAppId = "1309967280842735738";
    private DiscordRpcClient _client;

    private static DiscordRPC.Logging.LogLevel _logLevel = DiscordRPC.Logging.LogLevel.Trace;
    private static int discordPipe = -1;

    private void Initialize()
    {
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
    }

    private void MainMenuManager_Set_MenuCondition(On.MainMenuManager.orig_Set_MenuCondition orig, MainMenuManager self, int _index)
    {
        orig(self, _index);
        SetPresence(MainMenuState());
    }

    private void Player_OnPlayerMapInstanceChange(On.Player.orig_OnPlayerMapInstanceChange orig, Player self, MapInstance _old, MapInstance _new)
    {
        orig(self, _old, _new);

        string area = _new._mapName;
        string characterName = self._nickname;

        SetPresence(AreaState(characterName, area));
    }

    private void SetPresence(string state)
    {
        _client.SetPresence(new RichPresence()
        {
            Details = "Playing Atlyss",
            State = state,
            Assets = new Assets()
            {
                LargeImageKey = "atlyss_icon",
                LargeImageText = "ATLYSS",
                SmallImageKey = "atlyss_icon"
            }
        });
    }

    private void Awake()
    {
        UnityEngine.Debug.Log("Hello from Discord RPC!");
        Initialize();
    }

    private void OnDestroy()
    {
        _client.Dispose();
    }

    private string MainMenuState()
    {
        return "In Main Menu";
    }

    private string AreaState(string characterName, string area)
    {
        return $"Playing as {characterName} in {area}";
    }
}