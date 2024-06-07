using Unity.Netcode;
using UnityEngine;

#nullable enable
namespace CustomDeathAudio
{
    internal class AudioNetworkHandler : NetworkBehaviour
    {
        public static GameObject? AudioObject;

        private void Awake()
        {
            Plugin.AddLog("Awaking AudioNetworkHandler");
            AudioObject = null;
            DontDestroyOnLoad(gameObject);
            Plugin.AddLog("AudioNetworkHandler is awake");
        }

        public void Destroy()
        {
            DespawnNetObjServerRpc();
        }

        [ServerRpc]
        public void SpawnNetObjServerRpc()
        {
            Plugin.AddLog("Running SpawnNetObjServerRpc");

            AudioObject = Spawn(Plugin.NetObj);
        }

        public static GameObject Spawn(GameObject prefeb)
        {
            GameObject netObj = Instantiate(prefeb);

            NetworkObject component = netObj.GetComponent<NetworkObject>();
            if (component != null)
            {
                component.Spawn();
                Plugin.AddLog($"NetworkObject spawned.");
            }
            else
            {
                Plugin.AddLog("NetworkObject could not be spawned.");
            }

            return netObj;
        }

        [ServerRpc]
        public void PlayAudioServerRpc(float volume, float pitch, Vector3 position)
        {
            PlayAudioClientRpc(AudioObject.GetComponent<NetworkObject>().NetworkObjectId, volume, pitch, position);
        }

        [ClientRpc]
        public void PlayAudioClientRpc(ulong audioId, float volume, float pitch, Vector3 position)
        {
            Plugin.AddLog($"Running PlayAudioClientRpc, volume: {volume}, pitch: {pitch}, position: {position}");

            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(audioId, out NetworkObject audioObject);
            audioObject.transform.position = position;
            var source = audioObject.GetComponent<AudioSource>();
            Plugin.AddLog($"audioObject: {audioObject != null}, source: {source != null}");
            source.spatialBlend = 1;
            source.pitch = pitch;
            source.PlayOneShot(Plugin.CustomAudioClip, volume);
            Plugin.AddLog($"Audio played.");
        }

        [ServerRpc(RequireOwnership = false)]
        public void DespawnNetObjServerRpc()
        {
            Plugin.AddLog("Running DespawnNetObjServerRpc");
            AudioObject.GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
