using HarmonyLib;
using UnityEngine;

namespace Marioalexsan.ModAudio.HarmonyPatches;

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayHelper))]
static class AudioSource_PlayHelper
{
    static void Prefix(AudioSource source) => AudioEngine.AudioPlayed(source);
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), typeof(double))]
static class AudioSource_Play
{
    static void Prefix(AudioSource __instance) => AudioEngine.AudioPlayed(__instance);
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayOneShotHelper))]
static class AudioSource_PlayOneShotHelper
{
    static void Prefix(AudioSource source, ref AudioClip clip) => AudioEngine.ClipPlayed(ref clip, source);
}
