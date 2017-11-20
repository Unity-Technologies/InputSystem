using System;
using UnityEngine.Networking.PlayerConnection;

namespace ISX.Remote
{
    // Transports input remoting messages from and to players. Can be used to
    // make input on either side fully available on the other side. I.e. player
    // input can be fully debugged in the editor and editor input can conversely
    // be fed into the player.
    [Serializable]
    internal class RemoteInputPlayerConnection : IObserver<InputRemoting.Message>, IObservable<InputRemoting.Message>
    {
        public static readonly Guid kNewDeviceMsg = new Guid("fcd9651ded40425995dfa6aeb78f1f1c");
        public static readonly Guid kNewTemplateMsg = new Guid("fccfec2b7369466d88502a9dd38505f4");
        public static readonly Guid kNewEventsMsg = new Guid("34d9b47f923142ff847c0d1f8b0554d9");
        public static readonly Guid kRemoveDeviceMsg = new Guid("e5e299b2d9e44255b8990bb71af8922d");
        public static readonly Guid kChangeUsagesMsg = new Guid("b9fe706dfc854d7ca109a5e38d7db730");

        public void Connect(IEditorPlayerConnection connection)
        {
            if (m_Connection != null)
            {
                if (m_Connection == connection)
                    return;
                throw new InvalidOperationException("Already connected");
            }

            connection.RegisterConnection(OnConnected);
            connection.RegisterDisconnection(OnDisconnected);

            connection.Register(kNewDeviceMsg, OnNewDevice);
            connection.Register(kNewTemplateMsg, OnNewTemplate);
            connection.Register(kNewEventsMsg, OnNewEvents);
            connection.Register(kRemoveDeviceMsg, OnRemoveDevice);
            connection.Register(kChangeUsagesMsg, OnChangeUsages);
        }

        private void OnConnected(int id)
        {
        }

        private void OnDisconnected(int id)
        {
        }

        private void OnNewDevice(MessageEventArgs args)
        {
        }

        private void OnNewTemplate(MessageEventArgs args)
        {
        }

        private void OnNewEvents(MessageEventArgs args)
        {
        }

        private void OnRemoveDevice(MessageEventArgs args)
        {
        }

        private void OnChangeUsages(MessageEventArgs args)
        {
        }

        void IObserver<InputRemoting.Message>.OnNext(InputRemoting.Message msg)
        {
            if (m_Connection == null)
                return;

            switch (msg.type)
            {
                case InputRemoting.MessageType.NewDevice:
                    m_Connection.Send(kNewDeviceMsg, msg.data);
                    break;
                case InputRemoting.MessageType.NewTemplate:
                    m_Connection.Send(kNewTemplateMsg, msg.data);
                    break;
                case InputRemoting.MessageType.NewEvents:
                    m_Connection.Send(kNewEventsMsg, msg.data);
                    break;
                case InputRemoting.MessageType.ChangeUsages:
                    m_Connection.Send(kChangeUsagesMsg, msg.data);
                    break;
                case InputRemoting.MessageType.RemoveDevice:
                    m_Connection.Send(kRemoveDeviceMsg, msg.data);
                    break;
            }
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

        [NonSerialized] private IEditorPlayerConnection m_Connection;
        [NonSerialized] private Subscriber[] m_Subscribers;

        [Serializable]
        private class Player
        {
            public int playerId;
        }

        private class Subscriber : IDisposable
        {
            public RemoteInputPlayerConnection owner;
            public IObserver<InputRemoting.Message> observer;

            public void Dispose()
            {
            }
        }
    }
}
