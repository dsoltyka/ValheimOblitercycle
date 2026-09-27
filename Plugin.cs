using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using ValheimModShared;

namespace Oblitercycle
{
    /// <summary>
    /// Entry point. The Obliterator does its work on whichever peer owns it, so every peer needs the mod and the
    /// same settings: the config is marked admin-only, which makes Jötunn push the server's values to all clients.
    /// </summary>
    [BepInPlugin(PluginGuid, BuildInfo.Name, BuildInfo.Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dsoltyka.Oblitercycle";

        internal static ManualLogSource Log { get; private set; }
        internal static Settings Settings { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Settings = new Settings(Config);

            // Only a dedicated server should re-read its file live: clients receive the server's values instead.
            if (GUIManager.IsHeadless())
            {
                ConfigWatcher.Watch(Config, Log);
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(IncineratorPatch));

            Log.LogInfo($"{BuildInfo.Name} {BuildInfo.Version} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
