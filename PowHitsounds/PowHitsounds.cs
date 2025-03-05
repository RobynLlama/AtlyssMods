using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.PowHitsounds;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
[BepInDependency("Marioalexsan.ModAudio")]
public class PowHitsounds : BaseUnityPlugin
{
    public static PowHitsounds Instance { get; private set; }
    internal new ManualLogSource Logger { get; private set; }

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        ModAudio.ModAudio.Plugin.LoadModAudio(this);

        Logger.LogInfo("Initialized successfully!");
    }
}