using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

#nullable enable
namespace CustomDeathAudio
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void StartPatch(GameNetworkManager __instance)
        {
            if (Plugin.NetObj == null || Plugin.CustomAudioClip == null) return;
            NetworkManager.Singleton.AddNetworkPrefab(Plugin.NetObj);
            Plugin.AddLog("Adding network prefab");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void StartDisconnectPatch()
        {
            if (Plugin.NetObj != null && Plugin.CustomAudioClip != null)
            {
                RoundManagerPatch.AudioNetworkHandler.Destroy();
                Plugin.AddLog("Player disconnected.");
            }
        }
    }

    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch : NetworkBehaviour
    {
        public static AudioSource AudioSource;
        public static AudioNetworkHandler AudioNetworkHandler;

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void AwakePatch(RoundManager __instance)
        {
            AudioSource = __instance.gameObject.AddComponent<AudioSource>();
            AudioSource.spatialBlend = 0f;
            AudioSource.pitch = Plugin.CustomPitch.Value;
            if (Plugin.NetObj == null || Plugin.CustomAudioClip == null) return;
            AudioNetworkHandler = __instance.gameObject.AddComponent<AudioNetworkHandler>();
            Plugin.AddLog("RoundManagerPatch has awoken");
        }

        [HarmonyPatch("LoadNewLevelWait")]
        [HarmonyPrefix]
        public static void LoadNewLevelWaitPatch(RoundManager __instance)
        {
            Plugin.AddLog($"Spawning object...");

            if (Plugin.NetObj != null && Plugin.CustomAudioClip != null)
            {
                AudioNetworkHandler.SpawnNetObjServerRpc();
            }
        }

        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        [HarmonyPostfix]
        public static void DespawnPropsAtEndOfRoundPatch(RoundManager __instance)
        {
            Plugin.AddLog($"Despawning object...");

            if (Plugin.NetObj != null && Plugin.CustomAudioClip != null)
            {
                if (AudioNetworkHandler.IsServer)
                {
                    AudioNetworkHandler.DespawnNetObjServerRpc();
                }
            }
        }
    }


    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("KillPlayerClientRpc")]
        private static void KillPlayerPatch(PlayerControllerB __instance)
        {
            if (Plugin.NetObj != null && Plugin.CustomAudioClip != null)
            {
                if (__instance == Plugin.Player)
                    RoundManagerPatch.AudioSource.PlayOneShot(Plugin.CustomAudioClip, Plugin.Custom2DVolume.Value);
                if (RoundManagerPatch.AudioNetworkHandler.IsServer && Plugin.Player != null)
                {
                    var uid = Plugin.Player.NetworkObject.NetworkObjectId;
                    Plugin.AddLog(string.Format("Killing player: {0}, audio: {1}.", uid, Plugin.CustomAudioClip != null));

                    RoundManagerPatch.AudioNetworkHandler.PlayAudioServerRpc(
                        Plugin.CustomVolume.Value, Plugin.CustomPitch.Value, __instance.transform.position
                        );
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
            if (Plugin.NetObj == null) return;
            if (!__instance.IsOwner || Plugin.CustomAudioClip == null || !show)
                return;
            if (__instance != Plugin.Player)
                return;
            RoundManagerPatch.AudioSource.PlayOneShot(Plugin.CustomAudioClip, Plugin.Custom2DVolume.Value);
        }
    }
}
