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
using BepInEx.Configuration;
using System;
using System.Linq;

namespace CustomDeathAudio
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin? Instance;
        internal static ManualLogSource? LogSource;
        private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

        public AudioClip CustomSound = null;

        public static PlayerControllerB? Player => GameNetworkManager.Instance?.localPlayerController;

        private readonly string _pluginPath = $"BeanCan-{MyPluginInfo.PLUGIN_NAME}";
        private const string OriginFileName = "DeathAudio";

        internal static ConfigEntry<string> CustomRelativePath;
        internal static ConfigEntry<float> CustomVolume;
        internal static ConfigEntry<float> CustomPitch;

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

            CustomRelativePath = Config.Bind("General", "CustomRelatedPath", $"./{OriginFileName}.wav",
                "Customize the path of your audio relative to the plugin's folder. \n" +
                "e.g. \"../another-path/MyAudio.ogg\" \n" +
                "If no suffix is provided, \".wav\" will be considered as the default.");
            CustomVolume = Config.Bind("General", "CustomVolum", 1.0f, "Customize the Volum of your audio. \n" +
                "Should be a float number. (Default: 100%)");
            CustomPitch = Config.Bind("General", "CustomPitch", 1.0f, "Customize the pitch(playback speed) of your audio. \n" +
                "Should be a float number. (Default: 100%)");

            // Plugin startup logic
            AddLog($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        internal void Start() => this.ProcessSoundFile();

        internal void OnDestroy() => this.ProcessSoundFile();

        private void ProcessSoundFile()
        {
            if (_pluginPath == null) return;
            string relativePath = Path.GetDirectoryName(CustomRelativePath.Value);
            string fileName = Path.GetFileName(CustomRelativePath.Value);

            try
            {
                AddLog($"Loading {_pluginPath}/{relativePath}/{fileName}.");
                CustomSound = SoundTool.GetAudioClip(_pluginPath, relativePath, fileName);
            }
            catch { }

            if (CustomSound != null) return;

            try
            {
                AddLog($"Loading {_pluginPath}/{OriginFileName}.wav.");
                CustomSound = SoundTool.GetAudioClip(_pluginPath, $"{OriginFileName}.wav");
            }
            catch { }
            if (CustomSound != null) return;

            AddLog($"Failed to load the audio.");
        }
    }


    [HarmonyPatch(typeof(PlayerControllerB))]
    public class DeathPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("KillPlayer")]
        static void KillPlayerPatch(PlayerControllerB __instance)
        {
            Plugin.AddLog($"Killing player.");
            if (!__instance.IsOwner || Plugin.Instance?.CustomSound == null)
                return;
            Plugin.AddLog($"Playing death audio.");

            GameObject deathAudioObject = new("DeathAudioObject");
            var CustomSource = deathAudioObject.AddComponent<AudioSource>();
            CustomSource.pitch = Plugin.CustomPitch.Value;
            CustomSource.spatialBlend = 0;
            CustomSource.PlayOneShot(Plugin.Instance.CustomSound, Plugin.CustomVolume.Value);
        }
    }
}