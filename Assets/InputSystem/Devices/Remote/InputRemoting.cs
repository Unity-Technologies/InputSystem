using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngineInternal.Input;

////TODO: survive domain reloads

////TODO: reuse memory allocated for messages instead of allocating separately for each message

namespace ISX.Remote
{
    // Makes the activity and data of an InputManager observable in message form.
    // Can act as both the sender and receiver of these message so the flow is fully bidirectional,
    // i.e. the InputManager on either end can mirror its templates, devices, and events over
    // to the other end. This permits streaming input not just from the player to the editor but
    // also feeding input from the editor back into the player.
    public class InputRemoting : IObservable<InputRemoting.Message>, IObserver<InputRemoting.Message>
    {
        public const string kRemoteTemplateNamespacePrefix = "remote";

        public enum MessageType
        {
            Connect,
            Disconnect,
            NewTemplate,
            NewDevice,
            NewEvents,
        }

        public struct Message
        {
            public int sender;
            public MessageType type;
            public byte[] data;
        }

        ////TODO: interface to determine what to mirror from m_Manager to the remote system

        internal InputRemoting(InputManager manager, int senderId = 0)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            m_LocalManager = manager;
            m_SenderId = senderId;

            //when listening for newly added templates, must filter out ones we've added from remote
        }

        public void StartSending()
        {
            ////TODO: send events in bulk rather than one-by-one
            m_LocalManager.onEvent += SendEvent;
        }

        public void StopSending()
        {
            m_LocalManager.onEvent -= SendEvent;
        }

        void IObserver<Message>.OnNext(Message msg)
        {
            switch (msg.type)
            {
                case MessageType.NewTemplate:
                    NewTemplateMsg.Process(this, msg);
                    break;
                case MessageType.NewDevice:
                    NewDeviceMsg.Process(this, msg);
                    break;
                case MessageType.NewEvents:
                    NewEventsMsg.Process(this, msg);
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

            SendAllTemplatesTo(subscriber);
            SendAllDevicesTo(subscriber);

            return subscriber;
        }

        private void SendAllTemplatesTo(Subscriber subscriber)
        {
            var allTemplates = new List<string>();
            m_LocalManager.ListTemplates(allTemplates);

            foreach (var templateName in allTemplates)
                SendTemplateTo(subscriber, templateName);
        }

        private void SendTemplateTo(Subscriber subscriber, string templateName)
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
            subscriber.observer.OnNext(message);
        }

        private void SendAllDevicesTo(Subscriber subscriber)
        {
            var devices = m_LocalManager.devices;
            foreach (var device in devices)
                SendDeviceTo(subscriber, device);
        }

        private void SendDeviceTo(Subscriber subscriber, InputDevice device)
        {
            var message = NewDeviceMsg.Create(this, device);
            subscriber.observer.OnNext(message);
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

            // Send to all observers.
            foreach (var subscriber in m_Subscribers)
                subscriber.observer.OnNext(message);
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

        private int m_SenderId; // Our unique ID in the network of senders and receivers.
        private InputManager m_LocalManager; // Input system we mirror input from and to.
        private Subscriber[] m_Subscribers; // Receivers we send input to.
        private RemoteSender[] m_Senders; // Senders we receive input from.

        // Data we keep about a unique sender that we receive input data
        // from. We keep track of the templates and devices we added to
        // the local system.
        [Serializable]
        private struct RemoteSender
        {
            public int senderId;
            public string templateNamespace;
            public string[] templates;
            public RemoteInputDevice[] devices;
        }

        [Serializable]
        private struct RemoteInputDevice
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

        private class Subscriber : IDisposable
        {
            public InputRemoting owner;
            public IObserver<Message> observer;
            public void Dispose()
            {
                ArrayHelpers.Erase(ref owner.m_Subscribers, this);
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
                    sender = sender.m_SenderId,
                    type = MessageType.NewTemplate,
                    data = bytes
                };
            }

            public static void Process(InputRemoting receiver, Message msg)
            {
                var json = Encoding.UTF8.GetString(msg.data);
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.sender);
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
                    sender = sender.m_SenderId,
                    type = MessageType.NewDevice,
                    data = bytes
                };
            }

            public static void Process(InputRemoting receiver, Message msg)
            {
                var senderIndex = receiver.FindOrCreateSenderRecord(msg.sender);

                // Deserialize.
                var json = Encoding.UTF8.GetString(msg.data);
                var data = JsonUtility.FromJson<Data>(json);

                // Create device.
                var template = $"{receiver.m_Senders[senderIndex].templateNamespace}::{data.template}";
                InputDevice device;
                try
                {
                    device = receiver.m_LocalManager.AddDevice(template);
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
                    sender = sender.m_SenderId,
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
                    var senderIndex = receiver.FindOrCreateSenderRecord(msg.sender);
                    var localDevices = receiver.m_Senders[senderIndex].devices;
                    var numLocalDevices = localDevices.Length;

                    while (eventPtr.data.ToInt64() < dataEndPtr.ToInt64())
                    {
                        // Patch up device ID to refer to local device and send event.
                        var remoteDeviceId = eventPtr.deviceId;
                        var localDeviceId = InputDevice.kInvalidDeviceId;
                        for (var i = 0; i < numLocalDevices; ++i)
                        {
                            if (localDevices[i].remoteId == remoteDeviceId)
                            {
                                eventPtr.deviceId = localDevices[i].localId;
                                if (isConnectedToNative)
                                    ////TODO: add API to send events in bulk rather than one by one
                                    NativeInputSystem.SendInput(eventPtr.data);
                                break;
                            }
                        }

                        ++eventCount;
                        eventPtr = eventPtr.Next();
                    }

                    if (!isConnectedToNative)
                        receiver.m_LocalManager.OnNativeUpdate(NativeInputUpdateType.Dynamic, eventCount, new IntPtr(dataPtr));
                }
            }
        }
    }
}
