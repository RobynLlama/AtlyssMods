using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;

namespace Marioalexsan.ModAudio.HarmonyPatches;

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayHelper))]
static class AudioSource_PlayHelper
{
    static void Prefix(AudioSource source)
    {
        if (ModAudio.Instance.DetailedLogging)
            ModAudio.Instance.Logger.LogInfo($"PlayHelper {source?.name} | {source?.clip?.name}");

        ModAudio.Instance.Reroute(source);
    }
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), typeof(double))]
static class AudioSource_Play
{
    static void Prefix(AudioSource __instance)
    {
        if (ModAudio.Instance.DetailedLogging)
            ModAudio.Instance.Logger.LogInfo($"Play {__instance?.name} | {__instance?.clip?.name}");

        ModAudio.Instance.Reroute(__instance);
    }
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayOneShotHelper))]
static class AudioSource_PlayOneShotHelper
{
    static void Prefix(AudioSource source, ref AudioClip clip)
    {
        if (ModAudio.Instance.DetailedLogging)
            ModAudio.Instance.Logger.LogInfo($"PlayOneShotHelper {source?.name} | {clip?.name}");

        if (ModAudio.Instance.Resolve(clip, out var destination))
        {
            clip = destination;
        }
    }
}
