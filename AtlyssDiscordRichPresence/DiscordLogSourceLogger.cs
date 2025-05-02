using BepInEx.Logging;
using DiscordRPC.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marioalexsan.AtlyssDiscordRichPresence;

public class DiscordLogSourceLogger(ManualLogSource source, DiscordRPC.Logging.LogLevel level) : ILogger
{
    public DiscordRPC.Logging.LogLevel Level { get; set; } = level;

    public void Error(string message, params object[] args)
    {
        if (Level > DiscordRPC.Logging.LogLevel.Error) return;

        source.LogError($"[DiscordRPC:Error] {string.Format(message, args)}");
    }

    public void Info(string message, params object[] args)
    {
        if (Level > DiscordRPC.Logging.LogLevel.Info) return;

        source.LogInfo($"[DiscordRPC:Info] {string.Format(message, args)}");
    }

    public void Trace(string message, params object[] args)
    {
        if (Level > DiscordRPC.Logging.LogLevel.Trace) return;

        source.LogDebug($"[DiscordRPC:Trace] {string.Format(message, args)}");
    }

    public void Warning(string message, params object[] args)
    {
        if (Level > DiscordRPC.Logging.LogLevel.Warning) return;

        source.LogWarning($"[DiscordRPC:Warning] {string.Format(message, args)}");
    }
}
