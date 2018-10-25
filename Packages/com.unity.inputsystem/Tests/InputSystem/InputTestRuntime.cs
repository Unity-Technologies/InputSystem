using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Input.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// An implementation of <see cref="IInputRuntime"/> for use during tests.
    /// </summary>
    /// <remarks>
    /// This class is only available in the editor and in development players.
    ///
    /// The test runtime replaces the services usually supplied by <see cref="UnityEngineInternal.Input.NativeInputSystem"/>.
    /// </remarks>
    /// <seealso cref="InputTestFixture.testRuntime"/>
    public class InputTestRuntime : IInputRuntime, IDisposable
    {
        public unsafe delegate long DeviceCommandCallback(int deviceId, InputDeviceCommand* command);

        ~InputTestRuntime()
        {
            Dispose();
        }

        public int AllocateDeviceId()
        {
            var result = m_NextDeviceId;
            ++m_NextDeviceId;
            return result;
        }

        ////REVIEW: this behaves differently from NativeInputRuntime.Update() which allows a mask
        public unsafe void Update(InputUpdateType type)
        {
            lock (m_Lock)
            {
                if (m_NewDeviceDiscoveries != null && m_NewDeviceDiscoveries.Count > 0)
                {
                    if (onDeviceDiscovered != null)
                        foreach (var entry in m_NewDeviceDiscoveries)
                            onDeviceDiscovered(entry.Key, entry.Value);
                    m_NewDeviceDiscoveries.Clear();
                }

                if (onBeforeUpdate != null)
                {
                    onBeforeUpdate(type);
                }
                if (onUpdate != null)
                {
                    onUpdate(type, m_EventCount, (IntPtr)m_EventBuffer.GetUnsafePtr());
                }

                m_EventCount = 0;
                m_EventWritePosition = 0;
                ++frameCount;
            }
        }

        public unsafe void QueueEvent(IntPtr ptr)
        {
            var eventPtr = (InputEvent*)ptr;
            var eventSize = eventPtr->sizeInBytes;
            var alignedEventSize = NumberHelpers.AlignToMultiple(eventSize, 4);

            lock (m_Lock)
            {
                eventPtr->m_EventId = m_NextEventId;
                ++m_NextEventId;

                // Enlarge buffer, if we have to.
                if ((m_EventWritePosition + alignedEventSize) > m_EventBuffer.Length)
                {
                    var newBufferSize = m_EventBuffer.Length + Mathf.Max((int)alignedEventSize, 1024);
                    var newBuffer = new NativeArray<byte>(newBufferSize, Allocator.Persistent);
                    UnsafeUtility.MemCpy(newBuffer.GetUnsafePtr(), m_EventBuffer.GetUnsafePtr(), m_EventWritePosition);
                    m_EventBuffer.Dispose();
                    m_EventBuffer = newBuffer;
                }

                // Copy event.
                UnsafeUtility.MemCpy((byte*)m_EventBuffer.GetUnsafePtr() + m_EventWritePosition, ptr.ToPointer(), eventSize);
                m_EventWritePosition += (int)alignedEventSize;
                ++m_EventCount;
            }
        }

        public void SetDeviceCommandCallback(int deviceId, DeviceCommandCallback callback)
        {
            lock (m_Lock)
            {
                if (m_DeviceCommandCallbacks == null)
                    m_DeviceCommandCallbacks = new List<KeyValuePair<int, DeviceCommandCallback>>();
                m_DeviceCommandCallbacks.Add(new KeyValuePair<int, DeviceCommandCallback>(deviceId, callback));
            }
        }

        public void SetDeviceCommandCallback<TCommand>(int deviceId, TCommand result)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            bool? receivedCommand = null;
            unsafe
            {
                SetDeviceCommandCallback(deviceId,
                    (id, commandPtr) =>
                    {
                        if (commandPtr->type == result.GetTypeStatic())
                        {
                            Assert.That(receivedCommand.HasValue, Is.False);
                            receivedCommand = true;
                            UnsafeUtility.MemCpy(commandPtr, UnsafeUtility.AddressOf(ref result),
                                UnsafeUtility.SizeOf<TCommand>());
                            return InputDeviceCommand.kGenericSuccess;
                        }

                        return InputDeviceCommand.kGenericFailure;
                    });
            }
        }

        public unsafe long DeviceCommand(int deviceId, InputDeviceCommand* commandPtr)
        {
            lock (m_Lock)
            {
                if (m_DeviceCommandCallbacks != null)
                    foreach (var entry in m_DeviceCommandCallbacks)
                    {
                        if (entry.Key == deviceId)
                        {
                            return entry.Value(deviceId, commandPtr);
                        }
                    }
            }

            return -1;
        }

        public int ReportNewInputDevice(string deviceDescriptor, int deviceId = InputDevice.kInvalidDeviceId)
        {
            lock (m_Lock)
            {
                if (deviceId == InputDevice.kInvalidDeviceId)
                    deviceId = AllocateDeviceId();
                if (m_NewDeviceDiscoveries == null)
                    m_NewDeviceDiscoveries = new List<KeyValuePair<int, string>>();
                m_NewDeviceDiscoveries.Add(new KeyValuePair<int, string>(deviceId, deviceDescriptor));
                return deviceId;
            }
        }

        public int ReportNewInputDevice(InputDeviceDescription description, int deviceId = InputDevice.kInvalidDeviceId)
        {
            return ReportNewInputDevice(description.ToJson(), deviceId);
        }

        public Action<InputUpdateType, int, IntPtr> onUpdate { get; set; }
        public Action<InputUpdateType> onBeforeUpdate { get; set; }
        public Action<int, string> onDeviceDiscovered { get; set; }
        public Action onShutdown { get; set; }
        public float pollingFrequency { get; set; }
        public double currentTime { get; set; }
        public InputUpdateType updateMask { get; set; }
        public int frameCount { get; set; }

        public ScreenOrientation screenOrientation
        {
            set
            {
                m_ScreenOrientation = value;
            }

            get
            {
                return m_ScreenOrientation;
            }
        }

        public Vector2 screenSize
        {
            set
            {
                m_ScreenSize = value;
            }
            get
            {
                return m_ScreenSize;
            }
        }

        public void Dispose()
        {
            m_EventBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public double currentTimeOffsetToRealtimeSinceStartup
        {
            get { return m_CurrentTimeOffsetToRealtimeSinceStartup; }
            set
            {
                m_CurrentTimeOffsetToRealtimeSinceStartup = value;
                InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup = value;
            }
        }

        private int m_NextDeviceId = 1;
        private uint m_NextEventId = 1;
        private int m_EventCount;
        private int m_EventWritePosition;
        private NativeArray<byte> m_EventBuffer = new NativeArray<byte>(1024 * 1024, Allocator.Persistent);
        private List<KeyValuePair<int, string>> m_NewDeviceDiscoveries;
        internal List<KeyValuePair<int, DeviceCommandCallback>> m_DeviceCommandCallbacks;
        private object m_Lock = new object();
        private ScreenOrientation m_ScreenOrientation = ScreenOrientation.Portrait;
        private Vector2 m_ScreenSize = new Vector2(Screen.width, Screen.height);
        private double m_CurrentTimeOffsetToRealtimeSinceStartup;

        #if UNITY_ANALYTICS || UNITY_EDITOR

        public Action<string, int, int> onRegisterAnalyticsEvent { get; set; }
        public Action<string, object> onSendAnalyticsEvent { get; set; }

        public void RegisterAnalyticsEvent(string name, int maxPerHour, int maxPropertiesPerEvent)
        {
            if (onRegisterAnalyticsEvent != null)
                onRegisterAnalyticsEvent(name, maxPerHour, maxPropertiesPerEvent);
        }

        public void SendAnalyticsEvent(string name, object data)
        {
            if (onSendAnalyticsEvent != null)
                onSendAnalyticsEvent(name, data);
        }

        #endif // UNITY_ANALYTICS || UNITY_EDITOR
    }
}
