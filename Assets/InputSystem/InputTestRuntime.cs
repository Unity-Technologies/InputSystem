#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using ISX.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ISX
{
    using IOCTLCallback = Func<int, IntPtr, int, long>;

    /// <summary>
    /// An implementation of <see cref="IInputRuntime"/> for use during tests.
    /// </summary>
    /// <remarks>
    /// This class is only available in the editor and in development players.
    ///
    /// The test runtime replaces the services usually supplied by <see cref="UnityEngineInternal.Input.NativeInputSystem"/>.
    /// </remarks>
    public class InputTestRuntime : IInputRuntime, IDisposable
    {
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

        public void Update(InputUpdateType type)
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
                    onUpdate(type, m_EventCount, m_EventBuffer.GetUnsafePtr());
                }

                m_EventCount = 0;
                m_EventWritePosition = 0;
            }
        }

        public unsafe void QueueEvent(IntPtr ptr)
        {
            var eventPtr = (InputEvent*)ptr;
            var eventSize = eventPtr->sizeInBytes;

            lock (m_Lock)
            {
                eventPtr->m_EventId = m_NextEventId;
                ++m_NextEventId;

                // Enlarge buffer, if we have to.
                if ((m_EventWritePosition + eventSize) > m_EventBuffer.Length)
                {
                    var newBufferSize = m_EventBuffer.Length + Mathf.Max((int)eventSize, 1024);
                    var newBuffer = new NativeArray<byte>(newBufferSize, Allocator.Persistent);
                    UnsafeUtility.MemCpy(newBuffer.GetUnsafePtr(), m_EventBuffer.GetUnsafePtr(), (ulong)m_EventBuffer.Length);
                    m_EventBuffer.Dispose();
                    m_EventBuffer = newBuffer;
                }

                // Copy event.
                UnsafeUtility.MemCpy(new IntPtr(m_EventBuffer.GetUnsafePtr().ToInt64() + m_EventWritePosition), ptr, eventSize);
                m_EventWritePosition += (int)eventSize;
                ++m_EventCount;
            }
        }

        public void SetIOCTLCallback(int deviceId, IOCTLCallback callback)
        {
            lock (m_Lock)
            {
                if (m_IOCTLCallbacks == null)
                    m_IOCTLCallbacks = new List<KeyValuePair<int, IOCTLCallback>>();
                m_IOCTLCallbacks.Add(new KeyValuePair<int, IOCTLCallback>(deviceId, callback));
            }
        }

        public long IOCTL(int deviceId, int code, IntPtr buffer, int size)
        {
            lock (m_Lock)
            {
                if (m_IOCTLCallbacks != null)
                    foreach (var entry in m_IOCTLCallbacks)
                    {
                        if (entry.Key == deviceId)
                            return entry.Value(code, buffer, size);
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

        public Action<InputUpdateType, int, IntPtr> onUpdate { get; set; }
        public Action<InputUpdateType> onBeforeUpdate { get; set; }
        public Action<int, string> onDeviceDiscovered { get; set; }
        public float PollingFrequency { get; set; }

        public void Dispose()
        {
            m_EventBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        private int m_NextDeviceId = 1;
        private uint m_NextEventId = 1;
        private int m_EventCount;
        private int m_EventWritePosition;
        private NativeArray<byte> m_EventBuffer = new NativeArray<byte>(1024 * 1024, Allocator.Persistent);
        private List<KeyValuePair<int, string>> m_NewDeviceDiscoveries;
        private List<KeyValuePair<int, IOCTLCallback>> m_IOCTLCallbacks;
        private object m_Lock = new object();
    }
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
