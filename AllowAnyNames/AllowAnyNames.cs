using BepInEx;
using HarmonyLib;

namespace Marioalexsan.AllowAnyNames;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public class AllowAnyNames : BaseUnityPlugin
{
    private readonly Harmony _harmony = new Harmony(ModInfo.PLUGIN_GUID);

    private void Awake()
    {
        _harmony.PatchAll();
        UnityEngine.Debug.Log($"{ModInfo.PLUGIN_GUID} initialized!");
    }
}