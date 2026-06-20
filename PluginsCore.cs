using BepInEx;
using EFT;
using HarmonyLib;
using System.IO;
using System.Reflection;

namespace ImmersiveRaidTime
{
    [BepInPlugin(PluginsInfo.GUID, PluginsInfo.NAME, PluginsInfo.VERSION)]
    public class PluginsCore : BaseUnityPlugin
    {
        public static Player CorrectPlayer { get; set; }
        public static string CorrectGroupId { get; set; }
        public static GameWorld CorrectGameWorld { get; set; }
        public static string dllPath = Assembly.GetExecutingAssembly().Location;
        public static string pluginDir = Path.GetDirectoryName(dllPath);
        public void Awake()
        {
            var harmony = new Harmony(PluginsInfo.GUID);
            harmony.PatchAll();
            CfgLocaleManager.Initialize(Config);
            CfgManager.Initialize(Config);
        }
        public void Start()
        {
        }
        public void Update()
        {
        }
        public void OnGUI()
        {
        }
    }
    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    public class GameStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameWorld __instance)
        {
            PluginsCore.CorrectGameWorld = __instance;
            PluginsCore.CorrectPlayer = __instance.MainPlayer;
            PluginsCore.CorrectGroupId = __instance.MainPlayer.Profile?.Info?.GroupId ?? "";
        }
    }
}
