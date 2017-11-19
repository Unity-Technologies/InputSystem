using System;
using UnityEngine.Networking.PlayerConnection;

namespace ISX.Remote
{
    // Transports input remoting messages from and to players. Can be used to
    // make input on either side fully available on the other side. I.e. player
    // input can be fully debugged in the editor and editor input can conversely
    // be fed into the player.
    [Serializable]
    internal class RemoteInputNetworkTransportToPlayer : IObserver<InputRemoting.Message>, IObservable<InputRemoting.Message>
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

        void IObserver<InputRemoting.Message>.OnNext(InputRemoting.Message value)
        {
        }

        void IObserver<InputRemoting.Message>.OnError(Exception error)
        {
        }

        void IObserver<InputRemoting.Message>.OnCompleted()
        {
        }

        public IDisposable Subscribe(IObserver<InputRemoting.Message> observer)
        {
            var subscriber = new Subscriber {owner = this, observer = observer};
            ArrayHelpers.Append(ref m_Subscribers, subscriber);
            return subscriber;
        }

        [NonSerialized] private Subscriber[] m_Subscribers;

        [Serializable]
        private class Player
        {
            public int playerId;
        }

        private class Subscriber : IDisposable
        {
            public RemoteInputNetworkTransportToPlayer owner;
            public IObserver<InputRemoting.Message> observer;

            public void Dispose()
            {
            }
        }
    }
}
