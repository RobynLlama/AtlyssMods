using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Marioalexsan.ModAudio.HarmonyPatches;

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayHelper))]
static class AudioSource_PlayHelper
{
    static void Prefix(AudioSource source)
    {
        //Debug.Log("PlayHelper " + source?.name + " " + source?.clip?.name);
        ModAudio.Instance.Reroute(source);
    }
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.Play), typeof(double))]
static class AudioSource_Play
{
    static void Prefix(AudioSource __instance)
    {
        //Debug.Log("Play " + __instance?.name + " " + __instance?.clip?.name);
        ModAudio.Instance.Reroute(__instance);
    }
}

[HarmonyPatch(typeof(AudioSource), nameof(AudioSource.PlayOneShotHelper))]
static class AudioSource_PlayOneShotHelper
{
    static void Prefix(AudioSource source)
    {
        //Debug.Log("PlayOneShotHelper " + source?.name + " " + source?.clip?.name);
        ModAudio.Instance.Reroute(source);
    }
}
