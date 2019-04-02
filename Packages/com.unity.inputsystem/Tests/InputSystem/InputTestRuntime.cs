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
    /// <seealso cref="InputTestFixture.runtime"/>
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

                onBeforeUpdate?.Invoke(type);

                // Advance time *after* onBeforeUpdate so that events generated from onBeforeUpdate
                // don't get bumped into the following update.
                if (type == InputUpdateType.Dynamic)
                    currentTime += advanceTimeEachDynamicUpdate;

                if (onUpdate != null)
                {
                    var buffer = new InputEventBuffer(
                        (InputEvent*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_EventBuffer),
                        m_EventCount, m_EventWritePosition, m_EventBuffer.Length);

                    onUpdate(type, ref buffer);

                    #if UNITY_2019_1_OR_NEWER
                    m_EventCount = buffer.eventCount;
                    m_EventWritePosition = (int)buffer.sizeInBytes;
                    if (NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(buffer.data) !=
                        NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_EventBuffer))
                        m_EventBuffer = buffer.data;
                    #else
                    if (type != InputUpdateType.BeforeRender)
                    {
                        m_EventCount = 0;
                        m_EventWritePosition = 0;
                    }
                    #endif
                }
                else
                {
                    m_EventCount = 0;
                    m_EventWritePosition = 0;
                }

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
                eventPtr->eventId = m_NextEventId;
                eventPtr->handled = false;
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

        public void SetDeviceCommandCallback(InputDevice device, DeviceCommandCallback callback)
        {
            SetDeviceCommandCallback(device.id, callback);
        }

        public void SetDeviceCommandCallback(int deviceId, DeviceCommandCallback callback)
        {
            lock (m_Lock)
            {
                if (m_DeviceCommandCallbacks == null)
                    m_DeviceCommandCallbacks = new List<KeyValuePair<int, DeviceCommandCallback>>();
                else
                {
                    for (var i = 0; i < m_DeviceCommandCallbacks.Count; ++i)
                    {
                        if (m_DeviceCommandCallbacks[i].Key == deviceId)
                        {
                            m_DeviceCommandCallbacks[i] = new KeyValuePair<int, DeviceCommandCallback>(deviceId, callback);
                            return;
                        }
                    }
                }
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
                if (commandPtr->type == QueryPairedUserAccountCommand.Type)
                {
                    foreach (var pairing in userAccountPairings)
                    {
                        if (pairing.deviceId != deviceId)
                            continue;

                        var queryPairedUser = (QueryPairedUserAccountCommand*)commandPtr;
                        queryPairedUser->handle = pairing.userHandle;
                        queryPairedUser->name = pairing.userName;
                        queryPairedUser->id = pairing.userId;
                        return (long)QueryPairedUserAccountCommand.Result.DevicePairedToUserAccount;
                    }
                }

                var result = InputDeviceCommand.kGenericFailure;
                if (m_DeviceCommandCallbacks != null)
                    foreach (var entry in m_DeviceCommandCallbacks)
                    {
                        if (entry.Key == deviceId)
                        {
                            result = entry.Value(deviceId, commandPtr);
                            if (result >= 0)
                                return result;
                        }
                    }
                return result;
            }
        }

        public void InvokeFocusChanged(bool newFocusState)
        {
            onFocusChanged?.Invoke(newFocusState);
        }

        public void FocusLost()
        {
            InvokeFocusChanged(false);
        }

        public void FocusGained()
        {
            InvokeFocusChanged(true);
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

        public int ReportNewInputDevice(InputDeviceDescription description, int deviceId = InputDevice.kInvalidDeviceId,
            ulong userHandle = 0, string userName = null, string userId = null)
        {
            deviceId = ReportNewInputDevice(description.ToJson(), deviceId);

            // If we have user information, automatically set up
            if (userHandle != 0)
                AssociateInputDeviceWithUser(deviceId, userHandle, userName, userId);

            return deviceId;
        }

        public int ReportNewInputDevice<TDevice>(int deviceId = InputDevice.kInvalidDeviceId,
            ulong userHandle = 0, string userName = null, string userId = null)
            where TDevice : InputDevice
        {
            return ReportNewInputDevice(
                new InputDeviceDescription {deviceClass = typeof(TDevice).Name, interfaceName = "Test"}, deviceId,
                userHandle, userName, userId);
        }

        public unsafe void ReportInputDeviceRemoved(int deviceId)
        {
            var removeEvent = DeviceRemoveEvent.Create(deviceId);
            var removeEventPtr = UnsafeUtility.AddressOf(ref removeEvent);
            QueueEvent(new IntPtr(removeEventPtr));
        }

        public void ReportInputDeviceRemoved(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            ReportInputDeviceRemoved(device.id);
        }

        public void AssociateInputDeviceWithUser(int deviceId, ulong userHandle, string userName = null, string userId = null)
        {
            var existingIndex = -1;
            for (var i = 0; i < userAccountPairings.Count; ++i)
                if (userAccountPairings[i].deviceId == deviceId)
                {
                    existingIndex = i;
                    break;
                }

            if (userHandle == 0)
            {
                if (existingIndex != -1)
                    userAccountPairings.RemoveAt(existingIndex);
            }
            else if (existingIndex != -1)
            {
                userAccountPairings[existingIndex] =
                    new PairedUser
                {
                    deviceId = deviceId,
                    userHandle = userHandle,
                    userName = userName,
                    userId = userId,
                };
            }
            else
            {
                userAccountPairings.Add(
                    new PairedUser
                    {
                        deviceId = deviceId,
                        userHandle = userHandle,
                        userName = userName,
                        userId = userId,
                    });
            }
        }

        public void AssociateInputDeviceWithUser(InputDevice device, ulong userHandle, string userName = null, string userId = null)
        {
            AssociateInputDeviceWithUser(device.id, userHandle, userName, userId);
        }

        public struct PairedUser
        {
            public int deviceId;
            public ulong userHandle;
            public string userName;
            public string userId;
        }

        public InputUpdateDelegate onUpdate { get; set; }
        public Action<InputUpdateType> onBeforeUpdate { get; set; }
        public Func<InputUpdateType, bool> onShouldRunUpdate { get; set; }
        public Action<int, string> onDeviceDiscovered { get; set; }
        public Action onShutdown { get; set; }
        public Action<bool> onFocusChanged { get; set; }
        public float pollingFrequency { get; set; }
        public double currentTime { get; set; }
        public double currentTimeForFixedUpdate { get; set; }
        public bool shouldRunInBackground { get; set; }
        public int frameCount { get; set; }

        public double advanceTimeEachDynamicUpdate { get; set; } = 1.0 / 60;

        public ScreenOrientation screenOrientation { set; get; } = ScreenOrientation.Portrait;

        public Vector2 screenSize { set; get; } = new Vector2(Screen.width, Screen.height);

        public List<PairedUser> userAccountPairings
        {
            get
            {
                if (m_UserPairings == null)
                    m_UserPairings = new List<PairedUser>();
                return m_UserPairings;
            }
        }

        public void Dispose()
        {
            m_EventBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        public double currentTimeOffsetToRealtimeSinceStartup
        {
            get => m_CurrentTimeOffsetToRealtimeSinceStartup;
            set
            {
                m_CurrentTimeOffsetToRealtimeSinceStartup = value;
                InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup = value;
            }
        }

        private int m_NextDeviceId = 1;
        private int m_NextEventId = 1;
        private int m_EventCount;
        private int m_EventWritePosition;
        private NativeArray<byte> m_EventBuffer = new NativeArray<byte>(1024 * 1024, Allocator.Persistent);
        private List<PairedUser> m_UserPairings;
        private List<KeyValuePair<int, string>> m_NewDeviceDiscoveries;
        private List<KeyValuePair<int, DeviceCommandCallback>> m_DeviceCommandCallbacks;
        private object m_Lock = new object();
        private double m_CurrentTimeOffsetToRealtimeSinceStartup;

        #if UNITY_ANALYTICS || UNITY_EDITOR

        public Action<string, int, int> onRegisterAnalyticsEvent { get; set; }
        public Action<string, object> onSendAnalyticsEvent { get; set; }

        public void RegisterAnalyticsEvent(string name, int maxPerHour, int maxPropertiesPerEvent)
        {
            onRegisterAnalyticsEvent?.Invoke(name, maxPerHour, maxPropertiesPerEvent);
        }

        public void SendAnalyticsEvent(string name, object data)
        {
            onSendAnalyticsEvent?.Invoke(name, data);
        }

        #endif // UNITY_ANALYTICS || UNITY_EDITOR
    }
}
