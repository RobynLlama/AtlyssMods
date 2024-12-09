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

namespace Marioalexsan.TemplateMod;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class TemplateMod : BaseUnityPlugin
{
    public static TemplateMod Instance { get; private set; }
    internal new ManualLogSource Logger { get; private set; }

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        Logger.LogInfo("Patching methods...");
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        Logger.LogInfo("Configuring...");
        Configure();

        Logger.LogInfo("Initialized successfully!");
    }

    private void Configure()
    {
        // Add configuration options
    }
}