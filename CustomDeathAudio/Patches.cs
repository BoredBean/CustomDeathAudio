using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
namespace CustomDeathAudio
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        public static Dictionary<ulong, GameObject> AudioSources = [];

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void PlayerStartPatch(PlayerControllerB __instance)
        {
            if (Plugin.CustomAudioClip == null || AudioSources.TryGetValue(__instance.NetworkObjectId, out _)) return;
            var obj = new GameObject();
            var source = obj.AddComponent<AudioSource>();
            if (Plugin.CustomPitch != null) source.pitch = Plugin.CustomPitch.Value;
            AudioSources[__instance.NetworkObjectId] = obj;
            Plugin.AddLog($"Init audio source for player {__instance.NetworkObjectId}");
        }

        [HarmonyPrefix]
        [HarmonyPatch("KillPlayerClientRpc")]
        private static void KillPlayerPatch(PlayerControllerB __instance)
        {
            Plugin.AddLog($"Player being killed. {__instance.NetworkObjectId}");
            if (Plugin.CustomAudioClip != null && AudioSources.TryGetValue(__instance.NetworkObjectId, out var sourceObj))
            {
                var source = sourceObj.GetComponent<AudioSource>();
                if (__instance == Plugin.Player)
                {
                    source.spatialBlend = 0;
                    if (Plugin.Custom2DVolume != null)
                    {
                        source.PlayOneShot(Plugin.CustomAudioClip, Plugin.Custom2DVolume.Value);
                        WalkieTalkie.TransmitOneShotAudio(source, Plugin.CustomAudioClip, Plugin.Custom2DVolume.Value);
                    }
                    Plugin.AddLog("Playing audio for you.");
                }
                else
                {
                    sourceObj.transform.position = __instance.transform.position;
                    source.spatialBlend = 1;
                    if (Plugin.CustomVolume != null)
                    {
                        source.PlayOneShot(Plugin.CustomAudioClip, Plugin.CustomVolume.Value);
                        WalkieTalkie.TransmitOneShotAudio(source, Plugin.CustomAudioClip, Plugin.CustomVolume.Value);
                    }
                    Plugin.AddLog($"Playing audio for player {__instance.NetworkObjectId}.");
                }
            }
        }
    }

    internal class OtherPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "ShowPlayersFiredScreen")]
        private static void FirePlayerPatch(HUDManager __instance, bool show)
        {
            Plugin.AddLog("Firing player.");
            if (Plugin.CustomAudioClip == null || !show || Plugin.Player == null) return;
            var source = PlayerControllerBPatch.AudioSources[Plugin.Player.NetworkObjectId].GetComponent<AudioSource>();
            if (Plugin.Custom2DVolume != null) source.PlayOneShot(Plugin.CustomAudioClip, Plugin.Custom2DVolume.Value);
        }
    }
}
