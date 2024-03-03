#nullable enable
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GameNetcodeStuff;
using Instruments4Music;
using LCSoundTool;
using UnityEngine;

namespace CustomDeathAudio
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin? Instance;
        internal static ManualLogSource? LogSource;
        private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

        public AudioClip? CustomSound;

        private string? _directoryPath ;
        private const string FileName = "DeathAudio";
        public static PlayerControllerB? Player => GameNetworkManager.Instance?.localPlayerController;
        public static void AddLog(string str)
        {
            LogSource?.LogInfo(str);
        }

        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("More than 1 plugin instance.");
            }
            Instance = this;

            LogSource = this.Logger;

            // Plugin startup logic
            AddLog($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            _directoryPath = Path.GetDirectoryName(Paths.PluginPath) + $@"\plugins\BeanCan-{MyPluginInfo.PLUGIN_NAME}";

            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        internal void Start() => this.ProcessSoundFile();

        internal void OnDestroy() => this.ProcessSoundFile();

        private void ProcessSoundFile()
        {
            if (_directoryPath == null) return;
            string[] possibleExtensions = [".wav", ".mp3", ".ogg"];
            foreach (var extension in possibleExtensions)
            {
                var filePath = Path.Combine(_directoryPath, FileName + extension);
                AddLog($"Path: {filePath}\nExist: {File.Exists(filePath)}");
                if (!File.Exists(filePath)) continue;
                CustomSound = SoundTool.GetAudioClip(_directoryPath, "", filePath);
                AddLog($"Audio clip created!");
                break;
            }
        }
    }
	

    [HarmonyPatch(typeof(PlayerControllerB))]
    public class DeathPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("KillPlayer")]
        static void KillPlayerPatch(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner || __instance.isPlayerDead || !__instance.AllowPlayerDeath())
                return;
            GameObject deathAudioObject = new("DeathAudioObject");
            var audioSource = deathAudioObject.AddComponent<AudioSource>();
            audioSource.clip = Plugin.Instance?.CustomSound;
            audioSource.volume = 1.0f;
            audioSource.spatialBlend = 0;
            audioSource.Play();
        }
    }
}