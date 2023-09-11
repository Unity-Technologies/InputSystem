using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    internal partial class InputManager
    {
        // Indices correspond with those in m_Devices.
        internal StateChangeMonitorsForDevice[] m_StateChangeMonitors;
        private InlinedArray<StateChangeMonitorTimeout> m_StateChangeMonitorTimeouts;

        ////TODO: support combining monitors for bitfields
        public void AddStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex, uint groupIndex)
        {
            if (m_DevicesCount <= 0) return;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            // Allocate/reallocate monitor arrays, if necessary.
            // We lazy-sync it to array of devices.
            if (m_StateChangeMonitors == null)
                m_StateChangeMonitors = new StateChangeMonitorsForDevice[m_DevicesCount];
            else if (m_StateChangeMonitors.Length <= deviceIndex)
                Array.Resize(ref m_StateChangeMonitors, m_DevicesCount);

            // If we have removed monitors
            if (!isProcessingEvents && m_StateChangeMonitors[deviceIndex].needToCompactArrays)
                m_StateChangeMonitors[deviceIndex].CompactArrays();

            // Add record.
            m_StateChangeMonitors[deviceIndex].Add(control, monitor, monitorIndex, groupIndex);
        }

        private void RemoveStateChangeMonitors(InputDevice device)
        {
            if (m_StateChangeMonitors == null)
                return;

            var deviceIndex = device.m_DeviceIndex;
            Debug.Assert(deviceIndex != InputDevice.kInvalidDeviceIndex);

            if (deviceIndex >= m_StateChangeMonitors.Length)
                return;

            m_StateChangeMonitors[deviceIndex].Clear();

            // Clear timeouts pending on any control on the device.
            for (var i = 0; i < m_StateChangeMonitorTimeouts.length; ++i)
                if (m_StateChangeMonitorTimeouts[i].control?.device == device)
                    m_StateChangeMonitorTimeouts[i] = default;
        }

        public void RemoveStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex)
        {
            if (m_StateChangeMonitors == null)
                return;

            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;

            // Ignore if device has already been removed.
            if (deviceIndex == InputDevice.kInvalidDeviceIndex)
                return;

            // Ignore if there are no state monitors set up for the device.
            if (deviceIndex >= m_StateChangeMonitors.Length)
                return;

            m_StateChangeMonitors[deviceIndex].Remove(monitor, monitorIndex, isProcessingEvents);

            // Remove pending timeouts on the monitor.
            for (var i = 0; i < m_StateChangeMonitorTimeouts.length; ++i)
                if (m_StateChangeMonitorTimeouts[i].monitor == monitor &&
                    m_StateChangeMonitorTimeouts[i].monitorIndex == monitorIndex)
                    m_StateChangeMonitorTimeouts[i] = default;
        }

        public void AddStateChangeMonitorTimeout(InputControl control, IInputStateChangeMonitor monitor, double time, long monitorIndex, int timerIndex)
        {
            m_StateChangeMonitorTimeouts.Append(
                new StateChangeMonitorTimeout
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
            var timeoutCount = m_StateChangeMonitorTimeouts.length;
            for (var i = 0; i < timeoutCount; ++i)
            {
                ////REVIEW: can we avoid the repeated array lookups without copying the struct out?
                if (ReferenceEquals(m_StateChangeMonitorTimeouts[i].monitor, monitor)
                    && m_StateChangeMonitorTimeouts[i].monitorIndex == monitorIndex
                    && m_StateChangeMonitorTimeouts[i].timerIndex == timerIndex)
                {
                    m_StateChangeMonitorTimeouts[i] = default;
                    break;
                }
            }
        }

        private void SortStateChangeMonitorsIfNecessary(int deviceIndex)
        {
            if (m_StateChangeMonitors != null && deviceIndex < m_StateChangeMonitors.Length &&
                m_StateChangeMonitors[deviceIndex].needToUpdateOrderingOfMonitors)
                m_StateChangeMonitors[deviceIndex].SortMonitorsByIndex();
        }

        public void SignalStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor)
        {
            var device = control.device;
            var deviceIndex = device.m_DeviceIndex;

            ref var monitorsForDevice = ref m_StateChangeMonitors[deviceIndex];
            for (var i = 0; i < monitorsForDevice.signalled.length; ++i)
            {
                SortStateChangeMonitorsIfNecessary(i);

                ref var listener = ref monitorsForDevice.listeners[i];
                if (listener.control == control && listener.monitor == monitor)
                    monitorsForDevice.signalled.SetBit(i);
            }
        }

        public unsafe void FireStateChangeNotifications()
        {
            var time = m_Runtime.currentTime;
            var count = Math.Min(m_StateChangeMonitors.LengthSafe(), m_DevicesCount);
            for (var i = 0; i < count; ++i)
                FireStateChangeNotifications(i, time, null);
        }

        // Record for a timeout installed on a state change monitor.
        private struct StateChangeMonitorTimeout
        {
            public InputControl control;
            public double time;
            public IInputStateChangeMonitor monitor;
            public long monitorIndex;
            public int timerIndex;
        }

        // Maps a single control to an action interested in the control. If
        // multiple actions are interested in the same control, we will end up
        // processing the control repeatedly but we assume this is the exception
        // and so optimize for the case where there's only one action going to
        // a control.
        //
        // Split into two structures to keep data needed only when there is an
        // actual value change out of the data we need for doing the scanning.
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
                // NOTE: This method must only *append* to arrays. This way we can safely add data while traversing
                //       the arrays in FireStateChangeNotifications. Note that appending *may* mean that the arrays
                //       are switched to larger arrays.

                // Record listener.
                var listenerCount = signalled.length;
                ArrayHelpers.AppendWithCapacity(ref listeners, ref listenerCount,
                    new StateChangeMonitorListener
                    { monitor = monitor, monitorIndex = monitorIndex, groupIndex = groupIndex, control = control });

                // Record memory region.
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

            public void Clear()
            {
                // We don't actually release memory we've potentially allocated but rather just reset
                // our count to zero.
                listeners.Clear(count);
                signalled.SetLength(0);

                needToCompactArrays = false;
            }

            public void CompactArrays()
            {
                for (var i = count - 1; i >= 0; --i)
                {
                    var memoryRegion = memoryRegions[i];
                    if (memoryRegion.sizeInBits != 0)
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
                // Insertion sort.
                for (var i = 1; i < signalled.length; ++i)
                {
                    for (var j = i; j > 0; --j)
                    {
                        // Sort by complexities only to keep the sort stable
                        // i.e. don't reverse the order of controls which have the same complexity
                        var firstComplexity = InputActionState.GetComplexityFromMonitorIndex(listeners[j - 1].monitorIndex);
                        var secondComplexity = InputActionState.GetComplexityFromMonitorIndex(listeners[j].monitorIndex);
                        if (firstComplexity >= secondComplexity)
                            break;

                        listeners.SwapElements(j, j - 1);
                        memoryRegions.SwapElements(j, j - 1);

                        // We can ignore the `signalled` array here as we call this method only
                        // when all monitors are in non-signalled state.
                    }
                }

                needToUpdateOrderingOfMonitors = false;
            }
        }

        // NOTE: 'newState' can be a subset of the full state stored at 'oldState'. In this case,
        //       'newStateOffsetInBytes' must give the offset into the full state and 'newStateSizeInBytes' must
        //       give the size of memory slice to be updated.
        private unsafe bool ProcessStateChangeMonitors(int deviceIndex, void* newStateFromEvent, void* oldStateOfDevice, uint newStateSizeInBytes, uint newStateOffsetInBytes)
        {
            if (m_StateChangeMonitors == null)
                return false;

            // We resize the monitor arrays only when someone adds to them so they
            // may be out of sync with the size of m_Devices.
            if (deviceIndex >= m_StateChangeMonitors.Length)
                return false;

            var memoryRegions = m_StateChangeMonitors[deviceIndex].memoryRegions;
            if (memoryRegions == null)
                return false; // No one cares about state changes on this device.

            var numMonitors = m_StateChangeMonitors[deviceIndex].count;
            var signalled = false;
            var signals = m_StateChangeMonitors[deviceIndex].signalled;
            var haveChangedSignalsBitfield = false;

            // For every memory region that overlaps what we got in the event, compare memory contents
            // between the old device state and what's in the event. If the contents different, the
            // respective state monitor signals.
            var newEventMemoryRegion = new MemoryHelpers.BitRegion(newStateOffsetInBytes, 0, newStateSizeInBytes * 8);
            for (var i = 0; i < numMonitors; ++i)
            {
                var memoryRegion = memoryRegions[i];

                // Check if the monitor record has been wiped in the meantime. If so, remove it.
                if (memoryRegion.sizeInBits == 0)
                {
                    ////REVIEW: Do we really care? It is nice that it's predictable this way but hardly a hard requirement
                    // NOTE: We're using EraseAtWithCapacity here rather than EraseAtByMovingTail to preserve
                    //       order which makes the order of callbacks somewhat more predictable.

                    var listenerCount = numMonitors;
                    var memoryRegionCount = numMonitors;
                    m_StateChangeMonitors[deviceIndex].listeners.EraseAtWithCapacity(ref listenerCount, i);
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
                m_StateChangeMonitors[deviceIndex].signalled = signals;

            m_StateChangeMonitors[deviceIndex].needToCompactArrays = false;

            return signalled;
        }

        internal unsafe void FireStateChangeNotifications(int deviceIndex, double internalTime, InputEvent* eventPtr)
        {
            Debug.Assert(m_StateChangeMonitors != null);
            Debug.Assert(m_StateChangeMonitors.Length > deviceIndex);

            // NOTE: This method must be safe for mutating the state change monitor arrays from *within*
            //       NotifyControlStateChanged()! This includes all monitors for the device being wiped
            //       completely or arbitrary additions and removals having occurred.

            ref var signals = ref m_StateChangeMonitors[deviceIndex].signalled;
            ref var listeners = ref m_StateChangeMonitors[deviceIndex].listeners;
            var time = internalTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            // If we don't have an event, gives us as dummy, invalid instance.
            // What matters is that InputEventPtr.valid is false for these.
            var tempEvent = new InputEvent(new FourCC('F', 'A', 'K', 'E'), InputEvent.kBaseEventSize, -1, internalTime);
            if (eventPtr == null)
                eventPtr = (InputEvent*)UnsafeUtility.AddressOf(ref tempEvent);

            // Call IStateChangeMonitor.NotifyControlStateChange for every monitor that is in
            // signalled state.
            eventPtr->handled = false;
            for (var i = 0; i < signals.length; ++i)
            {
                if (!signals.TestBit(i))
                    continue;

                var listener = listeners[i];
                try
                {
                    listener.monitor.NotifyControlStateChanged(listener.control, time, eventPtr,
                        listener.monitorIndex);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Exception '{exception.GetType().Name}' thrown from state change monitor '{listener.monitor.GetType().Name}' on '{listener.control}'");
                    Debug.LogException(exception);
                }

                // If the monitor signalled that it has processed the state change, reset all signalled
                // state monitors in the same group. This is what causes "SHIFT+B" to prevent "B" from
                // also triggering.
                if (eventPtr->handled)
                {
                    var groupIndex = listeners[i].groupIndex;
                    for (var n = i + 1; n < signals.length; ++n)
                    {
                        // NOTE: We restrict the preemption logic here to a single monitor. Otherwise,
                        //       we will have to require that group indices are stable *between*
                        //       monitors. Two separate InputActionStates, for example, would have to
                        //       agree on group indices that valid *between* the two states or we end
                        //       up preempting unrelated inputs.
                        //
                        //       Note that this implies there there is *NO* preemption between singleton
                        //       InputActions. This isn't intuitive.
                        if (listeners[n].groupIndex == groupIndex && listeners[n].monitor == listener.monitor)
                            signals.ClearBit(n);
                    }

                    // Need to reset it back to false as we may have more signalled state monitors that
                    // aren't in the same group (i.e. have independent inputs).
                    eventPtr->handled = false;
                }

                signals.ClearBit(i);
            }
        }

        private void ProcessStateChangeMonitorTimeouts()
        {
            if (m_StateChangeMonitorTimeouts.length == 0)
                return;

            // Go through the list and both trigger expired timers and remove any irrelevant
            // ones by compacting the array.
            // NOTE: We do not actually release any memory we may have allocated.
            var currentTime = m_Runtime.currentTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            var remainingTimeoutCount = 0;
            for (var i = 0; i < m_StateChangeMonitorTimeouts.length; ++i)
            {
                // If we have reset this entry in RemoveStateChangeMonitorTimeouts(),
                // skip over it and let compaction get rid of it.
                if (m_StateChangeMonitorTimeouts[i].control == null)
                    continue;

                var timerExpirationTime = m_StateChangeMonitorTimeouts[i].time;
                if (timerExpirationTime <= currentTime)
                {
                    var timeout = m_StateChangeMonitorTimeouts[i];
                    timeout.monitor.NotifyTimerExpired(timeout.control,
                        currentTime, timeout.monitorIndex, timeout.timerIndex);

                    // Compaction will get rid of the entry.
                }
                else
                {
                    // Rather than repeatedly calling RemoveAt() and thus potentially
                    // moving the same data over and over again, we compact the array
                    // on the fly and move entries in the array down as needed.
                    if (i != remainingTimeoutCount)
                        m_StateChangeMonitorTimeouts[remainingTimeoutCount] = m_StateChangeMonitorTimeouts[i];
                    ++remainingTimeoutCount;
                }
            }

            m_StateChangeMonitorTimeouts.SetLength(remainingTimeoutCount);
        }
    }
}
