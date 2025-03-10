using HarmonyLib;
using Mirror;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Marioalexsan.ModAudio.HarmonyPatches;

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayHelper))]
static class AudioSource_PlayHelper
{
    static bool Prefix(AudioSource source) => AudioEngine.AudioPlayed(source);
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), typeof(double))]
static class AudioSource_Play
{
    static bool Prefix(AudioSource __instance) => AudioEngine.AudioPlayed(__instance);
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayOneShotHelper))]
static class AudioSource_PlayOneShotHelper
{
    static bool Prefix(AudioSource source, AudioClip clip) => AudioEngine.ClipPlayed(clip, source);
}