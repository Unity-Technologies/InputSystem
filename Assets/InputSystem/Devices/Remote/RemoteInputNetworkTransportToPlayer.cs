#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using UnityEngine.Networking.PlayerConnection;

namespace ISX.Remote
{
    // Makes input devices and activity from connected player visible in local
    // input system. Remote devices appear like local devices.
    // This means that remote devices are not just available for inspection but
    // can deliver actual input to the local system.
    [Serializable]
    internal class RemoteInputNetworkTransportToPlayer
    {
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
    }
}
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
