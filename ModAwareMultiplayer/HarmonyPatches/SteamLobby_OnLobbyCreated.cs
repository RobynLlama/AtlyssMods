using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marioalexsan.ModAwareMultiplayer.HarmonyPatches;

[HarmonyPatch(typeof(SteamLobby), nameof(SteamLobby.OnLobbyCreated))]
static class SteamLobby_OnLobbyCreated
{
    static void Postfix(LobbyCreated_t _callback)
    {
        CSteamID cSteamID = new CSteamID(_callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(cSteamID, "moddded", "true");
    }
}
