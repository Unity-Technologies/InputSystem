using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngineInternal.Input;

namespace ISX
{
    /// <summary>
    /// Makes the activity and data of an InputManager observable in message form.
    /// </summary>
    /// <remarks>
    /// Can act as both the sender and receiver of these message so the flow is fully bidirectional,
    /// i.e. the InputManager on either end can mirror its templates, devices, and events over
    /// to the other end. This permits streaming input not just from the player to the editor but
    /// also feeding input from the editor back into the player.
    ///
    /// Remoting sits entirely on top of the input system as an optional piece of functionality.
    /// In development players and the editor, we enable it automatically but in non-development
    /// players it has to be explicitly requested by the user.
    /// </remarks>
    /// <seealso cref="InputSystem.remoting"/>
    /// \todo Reuse memory allocated for messages instead of allocating separately for each message.
    /// \todo Inteface to determine what to mirror from the local manager to the remote system.
    public class InputRemoting : IObservable<InputRemoting.Message>, IObserver<InputRemoting.Message>
    {
        public const string kRemoteTemplateNamespacePrefix = "remote";

        /// <summary>
        /// Enumeration of possible types of messages exchanged between two InputRemoting instances.
        /// </summary>
        public enum MessageType
        {
            Connect,
            Disconnect,
            NewTemplate,
            NewDevice,
            NewEvents,
            RemoveDevice,
            RemoveTemplate,
            ChangeUsages,
        }

        /// <summary>
        /// A message exchanged between two InputRemoting instances.
        /// </summary>
        public struct Message
        {
            /// <summary>
            /// For messages coming in, numeric ID of the sender of the message. For messages
            /// going out, numeric ID of the targeted receiver of the message.
            /// </summary>
            public int participantId;
            public MessageType type;
            public byte[] data;
        }

        public bool sending
        {
            get { return (m_Flags & Flags.Sending) == Flags.Sending; }
            private set
            {
                if (value)
                    m_Flags |= Flags.Sending;
                else
                    m_Flags &= ~Flags.Sending;
            }
        }

        internal InputRemoting(InputManager manager, bool startSendingOnConnect = false)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            m_LocalManager = manager;

            if (startSendingOnConnect)
                m_Flags |= Flags.StartSendingOnConnect;

            //when listening for newly added templates, must filter out ones we've added from remote
        }

        /// <summary>
        /// Start sending messages for data and activity in the local input system
        /// to observers.
        /// </summary>
        /// <seealso cref="sending"/>
        /// <seealso cref="StopSending"/>
        public void StartSending()
        {
            if (sending)
                return;

            ////TODO: send events in bulk rather than one-by-one
            m_LocalManager.onEvent += SendEvent;
            m_LocalManager.onDeviceChange += SendDeviceChange;

            sending = true;

            SendAllCurrentData();
        }

        public void StopSending()
        {
            if (!sending)
                return;

            m_LocalManager.onEvent -= SendEvent;
            m_LocalManager.onDeviceChange -= SendDeviceChange;

            sending = false;
        }

        void IObserver<Message>.OnNext(Message msg)
        {
            switch (msg.type)
            {
                case MessageType.Connect:
                    ConnectMsg.Process(this, msg);
                    break;
                case MessageType.Disconnect:
                    DisconnectMsg.Process(this, msg);
                    break;
                case MessageType.NewTemplate:
                    NewTemplateMsg.Process(this, msg);
                    break;
                case MessageType.NewDevice:
                    NewDeviceMsg.Process(this, msg);
                    break;
                case MessageType.NewEvents:
                    NewEventsMsg.Process(this, msg);
                    break;
                case MessageType.ChangeUsages:
                    ChangeUsageMsg.Process(this, msg);
                    break;
                case MessageType.RemoveDevice:
                    RemoveDeviceMsg.Process(this, msg);
                    break;
            }
        }

        void IObserver<Message>.OnError(Exception error)
        {
        }

        void IObserver<Message>.OnCompleted()
        {
        }

        public IDisposable Subscribe(IObserver<Message> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            var subscriber = new Subscriber {owner = this, observer = observer};
            ArrayHelpers.Append(ref m_Subscribers, subscriber);

            return subscriber;
        }

        private void SendAllCurrentData(int recipientId = 0)
        {
            SendAllTemplates();
            SendAllDevices();
        }

        private void SendAllTemplates()
        {
            var allTemplates = new List<string>();
            m_LocalManager.ListTemplates(allTemplates);

            foreach (var templateName in allTemplates)
                SendTemplate(templateName);
        }

        private void SendTemplate(string templateName)
        {
            // Try to load the template. Ignore the template if it couldn't
            // be loaded.
            InputTemplate template;
            try
            {
                template = m_LocalManager.TryLoadTemplate(new InternedString(templateName));
            }
            catch (Exception exception)
            {
                Debug.Log($"Could not load template '{templateName}'; not sending to remote listeners (exception: {exception})");
                return;
            }

            // Send it.
            var message = NewTemplateMsg.Create(this, template);
            Send(message);
        }

        private void SendAllDevices()
        {
            var devices = m_LocalManager.devices;
            foreach (var device in devices)
                SendDevice(device);
        }

        private void SendDevice(InputDevice device)
        {
            var message = NewDeviceMsg.Create(this, device);
            Send(message);
        }

        private void SendEvent(InputEventPtr eventPtr)
        {
            if (m_Subscribers == null)
                return;

            var device = m_LocalManager.TryGetDeviceById(eventPtr.deviceId);

            ////REVIEW: we probably want to have better control over this and allow producing local events
            ////        against remote devices which *are* indeed sent across the wire
            // Don't send events that came in from remote devices.
            if (device != null && device.remote)
                return;

            var message = NewEventsMsg.Create(this, eventPtr.data, 1);
            Send(message);
        }

        private void SendDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (m_Subscribers == null)
                return;

            // Don't mirror remoted devices to other remotes.
            if (device.remote)
                return;

            Message msg;
            switch (change)
            {
                case InputDeviceChange.Added:
                    msg = NewDeviceMsg.Create(this, device);
                    break;
                case InputDeviceChange.Removed:
                    msg = RemoveDeviceMsg.Create(this, device);
                    break;
                case InputDeviceChange.UsageChanged:
                    msg = ChangeUsageMsg.Create(this, device);
                    break;
                default:
                    return;
            }

            Send(msg);
        }

        private void Send(Message msg)
        {
            foreach (var subscriber in m_Subscribers)
                subscriber.observer.OnNext(msg);
        }

        ////TODO: with C#7 this should be a ref return
        private int FindOrCreateSenderRecord(int senderId)
        {
            // Try to find existing.
            if (m_Senders != null)
            {
                var senderCount = m_Senders.Length;
                for (var i = 0; i < senderCount; ++i)
                    if (m_Senders[i].senderId == senderId)
                        return i;
            }

            // Create new.
            var sender = new RemoteSender
            {
                senderId = senderId,
                templateNamespace = $"{kRemoteTemplateNamespacePrefix}{senderId}"
            };
            return ArrayHelpers.Append(ref m_Senders, sender);
        }

        private int FindLocalDeviceId(int remoteDeviceId, int senderIndex)
        {
            var localDevices = m_Senders[senderIndex].devices;
            var numLocalDevices = localDevices.Length;

            for (var i = 0; i < numLocalDevices; ++i)
            {
                if (localDevices[i].remoteId == remoteDeviceId)
                    return localDevices[i].localId;
            }

            return InputDevice.kInvalidDeviceId;
        }

        private InputDevice TryGetDeviceByRemoteId(int remoteDeviceId, int senderIndex)
        {
            var localId = FindLocalDeviceId(remoteDeviceId, senderIndex);
            return m_LocalManager.TryGetDeviceById(localId);
        }

        private Flags m_Flags;
        private InputManager m_LocalManager; // Input system we mirror input from and to.
        private Subscriber[] m_Subscribers; // Receivers we send input to.
        private RemoteSender[] m_Senders; // Senders we receive input from.

        [Flags]
        private enum Flags
        {
            Sending = 1 << 0,
            StartSendingOnConnect = 1 << 1
        }

        // Data we keep about a unique sender that we receive input data
        // from. We keep track of the templates and devices we added to
        // the local system.
        [Serializable]
        internal struct RemoteSender
        {
            public int senderId;
            public string templateNamespace;
            public string[] templates;
            public RemoteInputDevice[] devices;
        }

        [Serializable]
        internal struct RemoteInputDevice
        {
            public int remoteId; // Device ID used by sender.
            public int localId; // Device ID used by us in local system.

            public InputDeviceDescription description;

            // Senders give us the full templates in JSON for the devices they
            // are using so we can recreate devices exactly like they appear
            // in the player. This also means that we don't need to have the same
            // templates available that the player does.
            //
            // When registering templates from remote senders, we prefix them
            // with "remote{senderId}::" to distinguish them from normal local
            // templates.
            public string templateName;
        }

        internal class Subscriber : IDisposable
        {
            public InputRemoting owner;
            public IObserver<Message> observer;
            public void Dispose()
            {
                ArrayHelpers.Erase(ref owner.m_Subscribers, this);
            }
        }

        private static class ConnectMsg
        {
            public static void Process(InputRemoting receiver, Message msg)
            {
                if (receiver.sending)
                    receiver.SendAllCurrentData(msg.participantId);
                else if ((receiver.m_Flags & Flags.StartSendingOnConnect) == Flags.StartSendingOnConnect)
                    receiver.StartSending();
            }
        }

        private static class DisconnectMsg
        {
            public static void Process(InputRemoting receiver, Message msg)
            {
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.participantId);

                // Remove devices added by remote.
                foreach (var remoteDevice in receiver.m_Senders[senderIndex].devices)
                {
                    var device = receiver.m_LocalManager.TryGetDeviceById(remoteDevice.localId);
                    if (device != null)
                        receiver.m_LocalManager.RemoveDevice(device);
                }

                ////TODO: remove templates added by remote

                ArrayHelpers.EraseAt(ref receiver.m_Senders, senderIndex);

                ////TODO: stop sending if last remote disconnects and StartSendingOnConnect is active
            }
        }

        // Tell remote input system that there's a new template.
        private static class NewTemplateMsg
        {
            public static Message Create(InputRemoting sender, InputTemplate template)
            {
                var json = template.ToJson();
                var bytes = Encoding.UTF8.GetBytes(json);

                return new Message
                {
                    type = MessageType.NewTemplate,
                    data = bytes
                };
            }

            public static void Process(InputRemoting receiver, Message msg)
            {
                var json = Encoding.UTF8.GetString(msg.data);
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.participantId);
                var @namespace = receiver.m_Senders[senderIndex].templateNamespace;

                receiver.m_LocalManager.RegisterTemplate(json, @namespace: @namespace);
                ArrayHelpers.Append(ref receiver.m_Senders[senderIndex].templates, json);
            }
        }

        // Tell remote input system that there's a new device.
        private static class NewDeviceMsg
        {
            [Serializable]
            public struct Data
            {
                public string name;
                public string template;
                public int deviceId;
                public InputDeviceDescription description;
            }

            public static Message Create(InputRemoting sender, InputDevice device)
            {
                Debug.Assert(!device.remote, "Device being sent to remotes should be a local devices, not a remote one");

                var data = new Data
                {
                    name = device.name,
                    template = device.template,
                    deviceId = device.id,
                    description = device.description
                };

                var json = JsonUtility.ToJson(data);
                var bytes = Encoding.UTF8.GetBytes(json);

                return new Message
                {
                    type = MessageType.NewDevice,
                    data = bytes
                };
            }

            public static void Process(InputRemoting receiver, Message msg)
            {
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.participantId);
                var data = DeserializeData<Data>(msg.data);

                // Create device.
                var template = $"{receiver.m_Senders[senderIndex].templateNamespace}::{data.template}";
                InputDevice device;
                try
                {
                    device = receiver.m_LocalManager.AddDevice(template, $"Remote{msg.participantId}::{data.name}");
                }
                catch (Exception exception)
                {
                    Debug.Log(
                        $"Could not create remote device '{data.description}' with template '{data.template}' locally (exception: {exception})");
                    return;
                }
                device.m_Description = data.description;
                device.m_Flags |= InputDevice.Flags.Remote;

                // Remember it.
                var record = new RemoteInputDevice
                {
                    remoteId = data.deviceId,
                    localId = device.id,
                    description = data.description,
                    templateName = template
                };
                ArrayHelpers.Append(ref receiver.m_Senders[senderIndex].devices, record);
            }
        }

        // Tell remote system there's new input events.
        private static class NewEventsMsg
        {
            public static unsafe Message Create(InputRemoting sender, IntPtr events, int eventCount)
            {
                // Find total size of event buffer we need.
                var totalSize = 0;
                var eventPtr = new InputEventPtr(events);
                for (var i = 0; i < eventCount; ++i, eventPtr = eventPtr.Next())
                {
                    totalSize += eventPtr.sizeInBytes;
                }

                // Copy event data to buffer. Would be nice if we didn't have to do that
                // but unfortunately we need a byte[] and can't just pass the 'events' IntPtr
                // directly.
                var data = new byte[totalSize];
                fixed(byte* dataPtr = data)
                {
                    UnsafeUtility.MemCpy(new IntPtr(dataPtr), events, totalSize);
                }

                // Done.
                return new Message
                {
                    type = MessageType.NewEvents,
                    data = data
                };
            }

            public static unsafe void Process(InputRemoting receiver, Message msg)
            {
                // This isn't the best solution but should do for now. NativeInputSystem isn't
                // designed for having multiple InputManagers and we only have that scenario
                // for tests ATM. So, to make InputManagers that aren't really properly connected
                // still work for testing, we directly feed them the events we get here.
                var isConnectedToNative = NativeInputSystem.onUpdate == receiver.m_LocalManager.OnNativeUpdate;

                fixed(byte* dataPtr = msg.data)
                {
                    var dataEndPtr = new IntPtr(dataPtr) + msg.data.Length;
                    var eventCount = 0;
                    var eventPtr = new InputEventPtr((InputEvent*)dataPtr);
                    var senderIndex = receiver.FindOrCreateSenderRecord(msg.participantId);

                    while (eventPtr.data.ToInt64() < dataEndPtr.ToInt64())
                    {
                        // Patch up device ID to refer to local device and send event.
                        var remoteDeviceId = eventPtr.deviceId;
                        var localDeviceId = receiver.FindLocalDeviceId(remoteDeviceId, senderIndex);
                        eventPtr.deviceId = localDeviceId;

                        if (localDeviceId != InputDevice.kInvalidDeviceId && isConnectedToNative)
                        {
                            ////TODO: add API to send events in bulk rather than one by one
                            NativeInputSystem.SendInput(eventPtr.data);
                        }

                        ++eventCount;
                        eventPtr = eventPtr.Next();
                    }

                    if (!isConnectedToNative)
                        receiver.m_LocalManager.OnNativeUpdate(NativeInputUpdateType.Dynamic, eventCount, new IntPtr(dataPtr));
                }
            }
        }

        private static class ChangeUsageMsg
        {
            [Serializable]
            public struct Data
            {
                public int deviceId;
                public string[] usages;
            }

            public static Message Create(InputRemoting sender, InputDevice device)
            {
                var data = new Data
                {
                    deviceId = device.id,
                    usages = device.usages.Select(x => x.ToString()).ToArray()
                };

                return new Message
                {
                    type = MessageType.ChangeUsages,
                    data = SerializeData(data)
                };
            }

            public static void Process(InputRemoting receiver, Message msg)
            {
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.participantId);
                var data = DeserializeData<Data>(msg.data);

                var device = receiver.TryGetDeviceByRemoteId(data.deviceId, senderIndex);
                if (device != null)
                {
                    ////TODO: clearing usages and setting multiple usages

                    if (data.usages.Length == 1)
                        receiver.m_LocalManager.SetUsage(device, new InternedString(data.usages[0]));
                }
            }
        }

        private static class RemoveDeviceMsg
        {
            public static Message Create(InputRemoting sender, InputDevice device)
            {
                return new Message
                {
                    type = MessageType.RemoveDevice,
                    data = BitConverter.GetBytes(device.id)
                };
            }

            public static void Process(InputRemoting receiver, Message msg)
            {
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.participantId);
                var remoteDeviceId = BitConverter.ToInt32(msg.data, 0);

                var device = receiver.TryGetDeviceByRemoteId(remoteDeviceId, senderIndex);
                if (device != null)
                    receiver.m_LocalManager.RemoveDevice(device);
            }
        }

        private static byte[] SerializeData<TData>(TData data)
        {
            var json = JsonUtility.ToJson(data);
            return Encoding.UTF8.GetBytes(json);
        }

        private static TData DeserializeData<TData>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<TData>(json);
        }

        // Domain reload survival. Kept separate as making the entire class [Serializable]
        // would signal the wrong thing to users as it's part of the public API.
#if UNITY_EDITOR
        // State we want to take across domain reloads. We can only take some of the
        // state across. Subscriptions will be lost and have to be manually restored.
        [Serializable]
        internal struct SerializedState
        {
            public int senderId;
            public RemoteSender[] senders;

            // We can't take these across domain reloads but we want to take them across
            // InputSystem.Save/Restore.
            [NonSerialized] public Subscriber[] subscribers;
        }

        internal SerializedState SaveState()
        {
            return new SerializedState
            {
                senders = m_Senders,
                subscribers = m_Subscribers
            };
        }

        internal void RestoreState(SerializedState state, InputManager manager)
        {
            m_LocalManager = manager;
            m_Senders = state.senders;
        }

#endif
    }
}
