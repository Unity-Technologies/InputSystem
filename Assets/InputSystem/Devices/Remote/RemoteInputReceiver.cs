#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace ISX
{
    // Makes input devices and activity from connected player visible in local
    // input system. Remote devices appear like local devices.
    // This means that remote devices are not just available for inspection but
    // can deliver actual input to the local system.
    [Serializable]
    internal class RemoteInputReceiver
    {
        [SerializeField] private RemotePlayer[] m_RemotePlayers;

        private void OnPlayerConnected(int playerId)
        {
        }

        private void OnPlayerDisconnected(int playerId)
        {
        }

        private void OnTemplateReceived(MessageEventArgs args)
        {
        }

        private void OnDeviceChangeReceived(MessageEventArgs args)
        {
        }

        private void OnInputReceived(MessageEventArgs args)
        {
        }

        [Serializable]
        private struct RemotePlayer
        {
            public int playerId;
            public RemoteInputDevice[] devices;
        }

        [Serializable]
        private struct RemoteInputDevice
        {
            public int remoteId; // Device ID in player.
            public int localId; // Device ID in editor.

            // Players send us the full templates in JSON for the devices they
            // are using so we can recreate devices exactly like they appear
            // in the player. This also means that we don't need to have the same
            // templates available that the player does.
            //
            // When registering templates from remote players, we prefix them
            // with the name of the player to distinguish them from normal local
            // templates.
            public string templateName;
        }
    }
}
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
