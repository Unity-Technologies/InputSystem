using System;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Networking.PlayerConnection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.InputSystem
{
    // Transports input remoting messages from and to players. Can be used to
    // make input on either side fully available on the other side. I.e. player
    // input can be fully debugged in the editor and editor input can conversely
    // be fed into the player.
    //
    // NOTE: The Unity EditorConnection/PlayerConnection mechanism requires this to
    //       be a ScriptableObject as it will register every listeners as a persistent
    //       one.
    [Serializable]
    internal class RemoteInputPlayerConnection :
#if UNITY_EDITOR
        // In the editor, we need to make sure that we get the same instance after domain reloads.
        // Otherwise, callbacks we have registered before the reload will no longer be valid, because
        // the object instance they point to will not deserialize to a valid object. So we use a
        // ScriptableSingleton instance, which fullfills these requirements. In the player, we need to
        // use a simple ScriptableObject, as ScriptableSingleton is an editor-only class.
        ScriptableSingleton<RemoteInputPlayerConnection>,
#else
        ScriptableObject,
#endif
        IObserver<InputRemoting.Message>, IObservable<InputRemoting.Message>
    {
        public static readonly Guid kNewDeviceMsg = new Guid("fcd9651ded40425995dfa6aeb78f1f1c");
        public static readonly Guid kNewLayoutMsg = new Guid("fccfec2b7369466d88502a9dd38505f4");
        public static readonly Guid kNewEventsMsg = new Guid("34d9b47f923142ff847c0d1f8b0554d9");
        public static readonly Guid kRemoveDeviceMsg = new Guid("e5e299b2d9e44255b8990bb71af8922d");
        public static readonly Guid kChangeUsagesMsg = new Guid("b9fe706dfc854d7ca109a5e38d7db730");
        public static readonly Guid kStartSendingMsg = new Guid("0d58e99045904672b3ef34b8797d23cb");
        public static readonly Guid kStopSendingMsg = new Guid("548716b2534a45369ab0c9323fc8b4a8");

        public void Bind(IEditorPlayerConnection connection, bool isConnected)
        {
            if (m_Connection != null)
            {
                if (m_Connection == connection)
                    return;
                throw new InvalidOperationException("Already bound to an IEditorPlayerConnection");
            }

            // If there's already connections on the given IEditorPlayerConnection,
            // calling RegisterConnection() will invoke the given callback for every
            // already existing connection. However, it seems to do so only in the
            // editor which is why we do the 'isConnected' dance below.
            connection.RegisterConnection(OnConnected);

            connection.RegisterDisconnection(OnDisconnected);

            connection.Register(kNewDeviceMsg, OnNewDevice);
            connection.Register(kNewLayoutMsg, OnNewLayout);
            connection.Register(kNewEventsMsg, OnNewEvents);
            connection.Register(kRemoveDeviceMsg, OnRemoveDevice);
            connection.Register(kChangeUsagesMsg, OnChangeUsages);

            connection.Register(kStartSendingMsg, OnStartSending);
            connection.Register(kStopSendingMsg, OnStopSending);

            m_Connection = connection;

            if (isConnected)
                OnConnected(0);
        }

        public IDisposable Subscribe(IObserver<InputRemoting.Message> observer)
        {
            if (observer == null)
                throw new System.ArgumentNullException(nameof(observer));

            var subscriber = new Subscriber {owner = this, observer = observer};
            ArrayHelpers.Append(ref m_Subscribers, subscriber);

            if (m_ConnectedIds != null)
            {
                foreach (var id in m_ConnectedIds)
                    observer.OnNext(new InputRemoting.Message { type = InputRemoting.MessageType.Connect, participantId = id });
            }

            return subscriber;
        }

        ////REVIEW: given that the PlayerConnection will connect to the editor regardless, we end up
        ////        on this path whether input remoting is enabled or not
        private void OnConnected(int id)
        {
            if (m_ConnectedIds != null && ArrayHelpers.Contains(m_ConnectedIds, id))
                return;

            ArrayHelpers.Append(ref m_ConnectedIds, id);

            SendToSubscribers(InputRemoting.MessageType.Connect, new MessageEventArgs {playerId = id});
        }

        private void OnDisconnected(int id)
        {
            if (m_ConnectedIds == null || !ArrayHelpers.Contains(m_ConnectedIds, id))
                return;

            ArrayHelpers.Erase(ref m_ConnectedIds, id);

            SendToSubscribers(InputRemoting.MessageType.Disconnect, new MessageEventArgs {playerId = id});
        }

        private void OnNewDevice(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.NewDevice, args);
        }

        private void OnNewLayout(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.NewLayout, args);
        }

        private void OnNewEvents(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.NewEvents, args);
        }

        private void OnRemoveDevice(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.RemoveDevice, args);
        }

        private void OnChangeUsages(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.ChangeUsages, args);
        }

        private void OnStartSending(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.StartSending, args);
        }

        private void OnStopSending(MessageEventArgs args)
        {
            SendToSubscribers(InputRemoting.MessageType.StopSending, args);
        }

        private void SendToSubscribers(InputRemoting.MessageType type, MessageEventArgs args)
        {
            if (m_Subscribers == null)
                return;

            var msg = new InputRemoting.Message
            {
                participantId = args.playerId,
                type = type,
                data = args.data
            };

            for (var i = 0; i < m_Subscribers.Length; ++i)
                m_Subscribers[i].observer.OnNext(msg);
        }

        void IObserver<InputRemoting.Message>.OnNext(InputRemoting.Message msg)
        {
            if (m_Connection == null)
                return;

            ////TODO: this should really be sending to a specific player in the editor (can't
            ////      do that through the IEditorPlayerConnection interface though)

            switch (msg.type)
            {
                case InputRemoting.MessageType.NewDevice:
                    m_Connection.Send(kNewDeviceMsg, msg.data);
                    break;
                case InputRemoting.MessageType.NewLayout:
                    m_Connection.Send(kNewLayoutMsg, msg.data);
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

        [SerializeField] private IEditorPlayerConnection m_Connection;
        [NonSerialized] private Subscriber[] m_Subscribers;
        [SerializeField] private int[] m_ConnectedIds;

        private class Subscriber : IDisposable
        {
            public RemoteInputPlayerConnection owner;
            public IObserver<InputRemoting.Message> observer;

            public void Dispose()
            {
                ArrayHelpers.Erase(ref owner.m_Subscribers, this);
            }
        }
    }
}
