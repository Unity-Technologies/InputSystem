using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: allow to restrict state change monitors to specific updates?

namespace UnityEngine.Experimental.Input.LowLevel
{
    using NotifyControlValueChangeAction = Action<InputControl, double, InputEventPtr, long>;
    using NotifyTimerExpiredAction = Action<InputControl, double, long, int>;

    /// <summary>
    /// Low-level APIs for working with input state memory.
    /// </summary>
    public static class InputState
    {
        /// <summary>
        /// The type of update that was last run or is currently being run on the input state.
        /// </summary>
        /// <remarks>
        /// This determines which set of buffers are currently active and thus determines which view code
        /// that queries input state will receive. For example, during editor updates, this will be
        /// <see cref="InputUpdateType.Editor"/> and the state buffers for the editor will be active.
        /// </remarks>
        public static InputUpdateType currentUpdate => InputUpdate.s_LastUpdateType;

        public static double currentTime => InputRuntime.s_Instance.currentTime + InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

        /// <summary>
        /// Callback that is triggered when the state of an input device changes.
        /// </summary>
        public static event Action<InputDevice> onChange
        {
            add => InputSystem.s_Manager.onDeviceStateChange += value;
            remove => InputSystem.s_Manager.onDeviceStateChange -= value;
        }

        /// <summary>
        /// Perform one update of input state.
        /// </summary>
        /// <remarks>
        /// Incorporates the given state and triggers all state change monitors as needed.
        /// </remarks>
        public static unsafe void Change<TState>(InputControl control, TState state, InputUpdateType updateType = default,
            InputEventPtr eventPtr = default)
            where TState : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (control.stateBlock.bitOffset != 0 || control.stateBlock.sizeInBits % 8 != 0)
                throw new ArgumentException($"Cannot change state of bitfield control '{control}' using this method");

            var device = control.device;
            var stateSize = UnsafeUtility.SizeOf<TState>();
            var statePtr = UnsafeUtility.AddressOf(ref state);
            var stateOffset = control.stateBlock.byteOffset - device.stateBlock.byteOffset;

            InputSystem.s_Manager.UpdateState(device,
                updateType != default ? updateType : InputSystem.s_Manager.defaultUpdateType, statePtr, stateOffset,
                (uint)stateSize,
                eventPtr.valid
                ? eventPtr.internalTime
                : InputRuntime.s_Instance.currentTime);
        }

        /// <summary>
        /// Perform a batch of changes one after the other against a single control.
        /// </summary>
        public static void BatchChange()
        {
        }

        public static bool IsIntegerFormat(this FourCC format)
        {
            return format == InputStateBlock.kTypeBit ||
                format == InputStateBlock.kTypeInt ||
                format == InputStateBlock.kTypeByte ||
                format == InputStateBlock.kTypeShort ||
                format == InputStateBlock.kTypeSBit ||
                format == InputStateBlock.kTypeUInt ||
                format == InputStateBlock.kTypeUShort ||
                format == InputStateBlock.kTypeLong ||
                format == InputStateBlock.kTypeULong;
        }

        ////REVIEW: should these take an InputUpdateType argument?

        public static void AddChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));
            if (control.device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new ArgumentException(string.Format("Device for control '{0}' has not been added to system"),
                    nameof(control));

            InputSystem.s_Manager.AddStateChangeMonitor(control, monitor, monitorIndex);
        }

        public static IInputStateChangeMonitor AddChangeMonitor(InputControl control, NotifyControlValueChangeAction valueChangeCallback, int monitorIndex = -1, NotifyTimerExpiredAction timerExpiredCallback = null)
        {
            if (valueChangeCallback == null)
                throw new ArgumentNullException(nameof(valueChangeCallback));
            var monitor = new StateChangeMonitorDelegate
            {
                valueChangeCallback = valueChangeCallback,
                timerExpiredCallback = timerExpiredCallback
            };
            AddChangeMonitor(control, monitor, monitorIndex);
            return monitor;
        }

        public static void RemoveChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            InputSystem.s_Manager.RemoveStateChangeMonitor(control, monitor, monitorIndex);
        }

        /// <summary>
        /// Put a timeout on a previously registered state change monitor.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="monitor"></param>
        /// <param name="time"></param>
        /// <param name="monitorIndex"></param>
        /// <param name="timerIndex"></param>
        /// <remarks>
        /// If by the given <paramref name="time"/>, no state change has been registered on the control monitored
        /// by the given <paramref name="monitor">state change monitor</paramref>, <see cref="IInputStateChangeMonitor.NotifyTimerExpired"/>
        /// will be called on <paramref name="monitor"/>. If a state change happens by the given <paramref name="time"/>,
        /// the monitor is notified as usual and the timer is automatically removed.
        /// </remarks>
        public static void AddChangeMonitorTimeout(InputControl control, IInputStateChangeMonitor monitor, double time, long monitorIndex = -1, int timerIndex = -1)
        {
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            InputSystem.s_Manager.AddStateChangeMonitorTimeout(control, monitor, time, monitorIndex, timerIndex);
        }

        public static void RemoveChangeMonitorTimeout(IInputStateChangeMonitor monitor, long monitorIndex = -1, int timerIndex = -1)
        {
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            InputSystem.s_Manager.RemoveStateChangeMonitorTimeout(monitor, monitorIndex, timerIndex);
        }

        private class StateChangeMonitorDelegate : IInputStateChangeMonitor
        {
            public NotifyControlValueChangeAction valueChangeCallback;
            public NotifyTimerExpiredAction timerExpiredCallback;

            public void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
            {
                valueChangeCallback(control, time, eventPtr, monitorIndex);
            }

            public void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
            {
                timerExpiredCallback?.Invoke(control, time, monitorIndex, timerIndex);
            }
        }
    }
}
