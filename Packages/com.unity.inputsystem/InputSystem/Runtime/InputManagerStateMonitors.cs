using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Tracks per-device state change monitors and timeouts for <see cref="InputManager"/>.
    /// </summary>
    internal sealed class InputManagerStateMonitors
    {
        private readonly Func<int> m_GetDeviceCount;
        private readonly Func<bool> m_IsProcessingEvents;
        private readonly IInputRuntime m_Runtime;

        // Indices correspond with those in InputManager's device array.
        internal StateChangeMonitorsForDevice[] m_MonitorsPerDevice;
        private InlinedArray<StateChangeMonitorTimeout> m_Timeouts;

        public InputManagerStateMonitors(Func<int> getDeviceCount, Func<bool> isProcessingEvents, IInputRuntime runtime)
        {
            m_GetDeviceCount = getDeviceCount ?? throw new ArgumentNullException(nameof(getDeviceCount));
            m_IsProcessingEvents = isProcessingEvents ?? throw new ArgumentNullException(nameof(isProcessingEvents));
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        /// <summary>
        /// Runtime instance this monitor collection was constructed with. Used to avoid recreating the collection
        /// when <see cref="InputManager.InstallRuntime"/> is called again with the same runtime (same object identity).
        /// </summary>
        internal IInputRuntime CapturedRuntime => m_Runtime;

        ////TODO: support combining monitors for bitfields
        public void AddStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex, uint groupIndex)
        {
            var devicesCount = m_GetDeviceCount();
            if (devicesCount <= 0)
                return;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            if (m_MonitorsPerDevice == null)
                m_MonitorsPerDevice = new StateChangeMonitorsForDevice[devicesCount];
            else if (m_MonitorsPerDevice.Length <= deviceIndex)
                Array.Resize(ref m_MonitorsPerDevice, devicesCount);

            if (!m_IsProcessingEvents() && m_MonitorsPerDevice[deviceIndex].needToCompactArrays)
                m_MonitorsPerDevice[deviceIndex].CompactArrays();

            m_MonitorsPerDevice[deviceIndex].Add(control, monitor, monitorIndex, groupIndex);
        }

        public void RemoveStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex)
        {
            if (m_MonitorsPerDevice == null)
                return;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;

            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                return;

            if (deviceIndex >= m_MonitorsPerDevice.Length)
                return;

            m_MonitorsPerDevice[deviceIndex].Remove(monitor, monitorIndex, m_IsProcessingEvents());

            for (var i = 0; i < m_Timeouts.length; ++i)
                if (m_Timeouts[i].monitor == monitor && m_Timeouts[i].monitorIndex == monitorIndex)
                    m_Timeouts[i] = default;
        }

        public void AddStateChangeMonitorTimeout(InputControl control, IInputStateChangeMonitor monitor, double time, long monitorIndex, int timerIndex)
        {
            m_Timeouts.Append(new StateChangeMonitorTimeout
            {
                control = control,
                time = time,
                monitor = monitor,
                monitorIndex = monitorIndex,
                timerIndex = timerIndex,
            });
        }

        public void RemoveStateChangeMonitorTimeout(IInputStateChangeMonitor monitor, long monitorIndex, int timerIndex)
        {
            var timeoutCount = m_Timeouts.length;
            for (var i = 0; i < timeoutCount; ++i)
            {
                if (ReferenceEquals(m_Timeouts[i].monitor, monitor)
                    && m_Timeouts[i].monitorIndex == monitorIndex
                    && m_Timeouts[i].timerIndex == timerIndex)
                {
                    m_Timeouts[i] = default;
                    break;
                }
            }
        }

        public void SignalStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor)
        {
            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;

            if (m_MonitorsPerDevice == null || deviceIndex >= m_MonitorsPerDevice.Length)
                return;

            SortMonitorsForDeviceIfNeeded(deviceIndex);

            ref var monitorsForDevice = ref m_MonitorsPerDevice[deviceIndex];
            for (var i = 0; i < monitorsForDevice.signalled.length; ++i)
            {
                ref var listener = ref monitorsForDevice.listeners[i];
                if (listener.control == control && listener.monitor == monitor)
                    monitorsForDevice.signalled.SetBit(i);
            }
        }

        public unsafe void FireStateChangeNotifications()
        {
            var time = m_Runtime.currentTime;
            var count = Math.Min(m_MonitorsPerDevice.LengthSafe(), m_GetDeviceCount());
            for (var i = 0; i < count; ++i)
                FireStateChangeNotifications(i, time, null);
        }

        internal void OnDeviceRemoved(InputDevice device)
        {
            ClearMonitorsAndTimeoutsForDevice(device);

            var deviceIndex = device.m_DeviceIndex;
            if (m_MonitorsPerDevice != null && deviceIndex < m_MonitorsPerDevice.LengthSafe())
            {
                var count = m_MonitorsPerDevice.Length;
                ArrayHelpers.EraseAtWithCapacity(m_MonitorsPerDevice, ref count, deviceIndex);
            }
        }

        internal void ProcessTimeouts()
        {
            if (m_Timeouts.length == 0)
                return;

            var currentTime = m_Runtime.currentTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            var remainingTimeoutCount = 0;
            for (var i = 0; i < m_Timeouts.length; ++i)
            {
                if (m_Timeouts[i].control == null)
                    continue;

                if (m_Timeouts[i].time <= currentTime)
                {
                    var timeout = m_Timeouts[i];
                    timeout.monitor.NotifyTimerExpired(timeout.control, currentTime, timeout.monitorIndex, timeout.timerIndex);
                }
                else
                {
                    if (i != remainingTimeoutCount)
                        m_Timeouts[remainingTimeoutCount] = m_Timeouts[i];
                    ++remainingTimeoutCount;
                }
            }

            m_Timeouts.SetLength(remainingTimeoutCount);
        }

        internal void SortMonitorsForDeviceIfNeeded(int deviceIndex)
        {
            if (m_MonitorsPerDevice != null && deviceIndex < m_MonitorsPerDevice.Length &&
                m_MonitorsPerDevice[deviceIndex].needToUpdateOrderingOfMonitors)
                m_MonitorsPerDevice[deviceIndex].SortMonitorsByIndex();
        }

        internal unsafe bool ProcessStateChange(int deviceIndex, void* newStateFromEvent, void* oldStateOfDevice, uint newStateSizeInBytes, uint newStateOffsetInBytes)
        {
            if (m_MonitorsPerDevice == null)
                return false;

            if (deviceIndex >= m_MonitorsPerDevice.Length)
                return false;

            var memoryRegions = m_MonitorsPerDevice[deviceIndex].memoryRegions;
            if (memoryRegions == null)
                return false;

            var numMonitors = m_MonitorsPerDevice[deviceIndex].count;
            var signalled = false;
            var signals = m_MonitorsPerDevice[deviceIndex].signalled;
            var haveChangedSignalsBitfield = false;

            var newEventMemoryRegion = new MemoryHelpers.BitRegion(newStateOffsetInBytes, 0, newStateSizeInBytes * 8);
            for (var i = 0; i < numMonitors; ++i)
            {
                var memoryRegion = memoryRegions[i];

                if (memoryRegion.sizeInBits == 0)
                {
                    var listenerCount = numMonitors;
                    var memoryRegionCount = numMonitors;
                    m_MonitorsPerDevice[deviceIndex].listeners.EraseAtWithCapacity(ref listenerCount, i);
                    memoryRegions.EraseAtWithCapacity(ref memoryRegionCount, i);
                    signals.SetLength(numMonitors - 1);
                    haveChangedSignalsBitfield = true;
                    --numMonitors;
                    --i;
                    continue;
                }

                var overlap = newEventMemoryRegion.Overlap(memoryRegion);
                if (overlap.isEmpty || MemoryHelpers.Compare(oldStateOfDevice, (byte*)newStateFromEvent - newStateOffsetInBytes, overlap))
                    continue;

                signals.SetBit(i);
                haveChangedSignalsBitfield = true;
                signalled = true;
            }

            if (haveChangedSignalsBitfield)
                m_MonitorsPerDevice[deviceIndex].signalled = signals;

            m_MonitorsPerDevice[deviceIndex].needToCompactArrays = false;

            return signalled;
        }

        internal unsafe void FireStateChangeNotifications(int deviceIndex, double internalTime, InputEvent* eventPtr)
        {
            if (m_MonitorsPerDevice == null)
            {
                Debug.Assert(false, "State change monitors array is null - has AddStateChangeMonitor been called?");
                return;
            }

            if (m_MonitorsPerDevice.Length <= deviceIndex)
            {
                Debug.Assert(false, $"deviceIndex {deviceIndex} passed to FireStateChangeNotifications is out of bounds (current length {m_MonitorsPerDevice.Length}).");
                return;
            }

            ref var signals = ref m_MonitorsPerDevice[deviceIndex].signalled;
            if (signals.AnyBitIsSet() && m_MonitorsPerDevice[deviceIndex].listeners == null)
            {
                Debug.Assert(false, $"A state change for device {deviceIndex} has been set, but list of listeners is null.");
                return;
            }

            ref var listeners = ref m_MonitorsPerDevice[deviceIndex].listeners;
            var time = internalTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var tempEvent = new InputEvent(new FourCC('F', 'A', 'K', 'E'), InputEvent.kBaseEventSize, -1, internalTime);
            if (eventPtr == null)
                eventPtr = (InputEvent*)UnsafeUtility.AddressOf(ref tempEvent);

            var previouslyHandled = eventPtr->handled;
            for (var i = 0; i < signals.length; ++i)
            {
                if (!signals.TestBit(i))
                    continue;

                var listener = listeners[i];
                try
                {
                    listener.monitor.NotifyControlStateChanged(listener.control, time, eventPtr, listener.monitorIndex);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Exception '{exception.GetType().Name}' thrown from state change monitor '{(listener.monitor?.GetType().Name ?? "null")}' on '{(listener.control?.ToString() ?? "null")}'");
                    Debug.LogException(exception);
                }

                if (!previouslyHandled && eventPtr->handled)
                {
                    var groupIndex = listeners[i].groupIndex;
                    if (InputSystem.settings.IsShortcutResolutionUsingActionPriority)
                    {
                        var handlerPriority = InputActionStateMonitorIndex.FromPacked(listener.monitorIndex).Priority;
                        for (var n = i + 1; n < signals.length; ++n)
                        {
                            if (listeners[n].groupIndex == groupIndex && listeners[n].monitor == listener.monitor)
                            {
                                var candidatePriority = InputActionStateMonitorIndex.FromPacked(listeners[n].monitorIndex).Priority;
                                if (candidatePriority < handlerPriority)
                                    signals.ClearBit(n);
                            }
                        }
                    }
                    else
                    {
                        for (var n = i + 1; n < signals.length; ++n)
                        {
                            if (listeners[n].groupIndex == groupIndex && listeners[n].monitor == listener.monitor)
                                signals.ClearBit(n);
                        }
                    }
                }

                if (eventPtr->handled)
                    eventPtr->handled = previouslyHandled;

                signals.ClearBit(i);
            }
        }

        private void ClearMonitorsAndTimeoutsForDevice(InputDevice device)
        {
            if (m_MonitorsPerDevice == null)
                return;

            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            if (deviceIndex >= m_MonitorsPerDevice.Length)
                return;

            m_MonitorsPerDevice[deviceIndex].Clear();

            for (var i = 0; i < m_Timeouts.length; ++i)
                if (m_Timeouts[i].control?.device == device)
                    m_Timeouts[i] = default;
        }

        private struct StateChangeMonitorTimeout
        {
            public InputControl control;
            public double time;
            public IInputStateChangeMonitor monitor;
            public long monitorIndex;
            public int timerIndex;
        }

        internal struct StateChangeMonitorListener
        {
            public InputControl control;
            public IInputStateChangeMonitor monitor;
            public long monitorIndex;
            public uint groupIndex;
        }

        internal struct StateChangeMonitorsForDevice
        {
            public MemoryHelpers.BitRegion[] memoryRegions;
            public StateChangeMonitorListener[] listeners;
            public DynamicBitfield signalled;
            public bool needToUpdateOrderingOfMonitors;
            public bool needToCompactArrays;

            public int count => signalled.length;

            public void Add(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex, uint groupIndex)
            {
                var listenerCount = signalled.length;
                ArrayHelpers.AppendWithCapacity(ref listeners, ref listenerCount,
                    new StateChangeMonitorListener
                    { monitor = monitor, monitorIndex = monitorIndex, groupIndex = groupIndex, control = control });

                ref var controlStateBlock = ref control.m_StateBlock;
                var memoryRegionCount = signalled.length;
                ArrayHelpers.AppendWithCapacity(ref memoryRegions, ref memoryRegionCount,
                    new MemoryHelpers.BitRegion(controlStateBlock.byteOffset - control.device.stateBlock.byteOffset,
                        controlStateBlock.bitOffset, controlStateBlock.sizeInBits));

                signalled.SetLength(signalled.length + 1);

                needToUpdateOrderingOfMonitors = true;
            }

            public void Remove(IInputStateChangeMonitor monitor, long monitorIndex, bool deferRemoval)
            {
                if (listeners == null)
                    return;

                for (var i = 0; i < signalled.length; ++i)
                {
                    if (ReferenceEquals(listeners[i].monitor, monitor) && listeners[i].monitorIndex == monitorIndex)
                    {
                        if (deferRemoval)
                        {
                            listeners[i] = default;
                            memoryRegions[i] = default;
                            signalled.ClearBit(i);
                            needToCompactArrays = true;
                        }
                        else
                        {
                            RemoveAt(i);
                        }

                        break;
                    }
                }
            }

            public void Clear()
            {
                listeners.Clear(count);
                signalled.SetLength(0);

                needToCompactArrays = false;
            }

            public void CompactArrays()
            {
                for (var i = count - 1; i >= 0; --i)
                {
                    if (memoryRegions[i].sizeInBits != 0)
                        continue;

                    RemoveAt(i);
                }

                needToCompactArrays = false;
            }

            private void RemoveAt(int i)
            {
                var numListeners = count;
                var numMemoryRegions = count;
                listeners.EraseAtWithCapacity(ref numListeners, i);
                memoryRegions.EraseAtWithCapacity(ref numMemoryRegions, i);
                signalled.SetLength(count - 1);
            }

            public void SortMonitorsByIndex()
            {
                var useActionPriority = InputSystem.settings.IsShortcutResolutionUsingActionPriority;
                for (var i = 1; i < signalled.length; ++i)
                {
                    for (var j = i; j > 0; --j)
                    {
                        if (useActionPriority)
                        {
                            var firstPriority = InputActionStateMonitorIndex.FromPacked(listeners[j - 1].monitorIndex).Priority;
                            var secondPriority = InputActionStateMonitorIndex.FromPacked(listeners[j].monitorIndex).Priority;
                            if (firstPriority >= secondPriority)
                                break;
                        }
                        else
                        {
                            // Sort by complexities only to keep the sort stable
                            // i.e. don't reverse the order of controls which have the same complexity
                            var firstComplexity = InputActionState.GetComplexityFromMonitorIndex(listeners[j - 1].monitorIndex);
                            var secondComplexity = InputActionState.GetComplexityFromMonitorIndex(listeners[j].monitorIndex);
                            if (firstComplexity >= secondComplexity)
                                break;
                        }

                        listeners.SwapElements(j, j - 1);
                        memoryRegions.SwapElements(j, j - 1);

                        // We can ignore the `signalled` array here as we call this method only
                        // when all monitors are in non-signalled state.
                    }
                }

                needToUpdateOrderingOfMonitors = false;
            }
        }
    }
}
