#nullable enable
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LCSoundTool;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CustomDeathAudio
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("LCSoundTool", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin? Instance;
        internal static ManualLogSource? LogSource;

        public static AudioClip? CustomAudioClip;

        public static PlayerControllerB? Player => GameNetworkManager.Instance?.localPlayerController;

        private const string PluginPath = $"BeanCan-{MyPluginInfo.PLUGIN_NAME}";
        private const string OriginFileName = "DeathAudio";

        internal static ConfigEntry<string>? CustomRelativePath;
        internal static ConfigEntry<float>? CustomVolume;
        internal static ConfigEntry<float>? Custom2DVolume;
        internal static ConfigEntry<float>? CustomPitch;

        public static void AddLog(string str)
        {
            LogSource?.LogInfo(str);
        }

        private void Awake()
        {
            if (Instance != null)
            {
                throw new Exception("More than 1 plugin instance.");
            }
            Instance = this;

            LogSource = Logger;

            CustomRelativePath = Config.Bind("General", "CustomRelatedPath", $"./{OriginFileName}.wav",
                "Customize the path of your audio relative to the plugin's folder. \n" +
                "e.g. \"../another-path/MyAudio.ogg\" \n" +
                "If no suffix is provided, \".wav\" will be considered as the default.");
            CustomVolume = Config.Bind("General", "CustomVolume", 1.0f,
                "Customize the Volume of the 3D audio. \n" +
                "Config this for other players' audio. \n" +
                "3D means the audio plays from the body's location. \n" +
                "Should be a float number. (Default: 100%)");
            Custom2DVolume = Config.Bind("General", "Custom2DVolume", 0.5f,
                "Customize the Volume of the 2D audio. \n" +
                "Config this for your audio. \n" +
                "2D audio only plays for the killed player. \n" +
                "Should be a float number. (Default: 50%)");
            CustomPitch = Config.Bind("General", "CustomPitch", 1.0f,
                "Customize the pitch(playback speed) of your audio. \n" +
                "Should be a float number. (Default: 100%)");

            AddLog($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
        }
        internal void Start()
        {
            AddLog("Start loading custom audio.");
            ProcessAudioFile();
        }
        internal void OnDestroy()
        {
            AddLog("Start loading custom audio.");
            ProcessAudioFile();
        }

        private static void ProcessAudioFile()
        {
            var customPath = CustomRelativePath?.Value ?? $"{OriginFileName}.wav";
            var relativePath = Path.GetDirectoryName(customPath);
            var fileName = Path.GetFileName(customPath);

            try
            {
                AddLog($"Loading {PluginPath}/{customPath}.");
                CustomAudioClip = SoundTool.GetAudioClip(PluginPath, relativePath, fileName);
            }
            catch (Exception e)
            {
                AddLog(e.Message);
            }

            if (CustomAudioClip != null)
            {
                AddLog("Audio clip loaded.");
                return;
            }

            try
            {
                AddLog($"Loading {PluginPath}/{OriginFileName}.wav.");
                CustomAudioClip = SoundTool.GetAudioClip(PluginPath, $"{OriginFileName}.wav");
            }
            catch (Exception e)
            {
                AddLog(e.Message);
            }

            if (CustomAudioClip != null)
            {
                AddLog("Audio clip loaded.");
                return;
            }

            AddLog("Failed to load the audio.");
        }
    }
}