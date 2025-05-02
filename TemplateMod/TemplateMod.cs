using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Marioalexsan.TemplateMod;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class TemplateMod : BaseUnityPlugin
{
    public static TemplateMod Plugin => _plugin ?? throw new InvalidOperationException($"{nameof(TemplateMod)} hasn't been initialized yet. Either wait until initialization, or check via ChainLoader instead.");
    private static TemplateMod? _plugin;

    internal new ManualLogSource Logger { get; private set; }

    private Harmony _harmony;

    public TemplateMod()
    {
        _plugin = this;
        Logger = base.Logger;
        _harmony = new Harmony(ModInfo.PLUGIN_GUID);
    }

    private void Awake()
    {
        Logger.LogInfo("Patching methods...");
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