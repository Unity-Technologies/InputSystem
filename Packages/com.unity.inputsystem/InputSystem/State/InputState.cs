using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

////TODO: method to get raw state pointer for device/control

////REVIEW: allow to restrict state change monitors to specific updates?

namespace UnityEngine.InputSystem.LowLevel
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
        public static InputUpdateType currentUpdateType => InputUpdate.s_LatestUpdateType;

        ////FIXME: ATM this does not work for editor updates
        /// <summary>
        /// The number of times the current input state has been updated.
        /// </summary>
        public static uint updateCount => InputUpdate.s_UpdateStepCount;

        public static double currentTime => InputRuntime.s_Instance.currentTime - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

        /// <summary>
        /// Callback that is triggered when the state of an input device changes.
        /// </summary>
        /// <remarks>
        /// The first parameter is the device whose state was changed the second parameter is the event
        /// that triggered the change in state. Note that the latter may be <c>null</c> in case the
        /// change was performed directly through <see cref="Change"/> rather than through an event.
        /// </remarks>
        public static event Action<InputDevice, InputEventPtr> onChange
        {
            add => InputSystem.s_Manager.onDeviceStateChange += value;
            remove => InputSystem.s_Manager.onDeviceStateChange -= value;
        }

        public static unsafe void Change(InputDevice device, InputEventPtr eventPtr, InputUpdateType updateType = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            // Make sure event is a StateEvent or DeltaStateEvent and has a format matching the device.
            FourCC stateFormat;
            var eventType = eventPtr.type;
            if (eventType == StateEvent.Type)
                stateFormat = StateEvent.FromUnchecked(eventPtr)->stateFormat;
            else if (eventType == DeltaStateEvent.Type)
                stateFormat = DeltaStateEvent.FromUnchecked(eventPtr)->stateFormat;
            else
            {
                #if UNITY_EDITOR
                InputSystem.s_Manager.m_Diagnostics?.OnEventFormatMismatch(eventPtr, device);
                #endif
                return;
            }

            if (stateFormat != device.stateBlock.format)
                throw new ArgumentException(
                    $"State format {stateFormat} from event does not match state format {device.stateBlock.format} of device {device}",
                    nameof(eventPtr));

            InputSystem.s_Manager.UpdateState(device, eventPtr,
                updateType != default ? updateType : InputSystem.s_Manager.defaultUpdateType);
        }

        /// <summary>
        /// Perform one update of input state.
        /// </summary>
        /// <remarks>
        /// Incorporates the given state and triggers all state change monitors as needed.
        ///
        /// Note that input state changes performed with this method will not be visible on remotes as they will bypass
        /// event processing. It is effectively equivalent to directly writing into input state memory except that it
        /// also performs related tasks such as checking state change monitors, flipping buffers, or making the respective
        /// device current.
        /// </remarks>
        public static void Change<TState>(InputControl control, TState state, InputUpdateType updateType = default,
            InputEventPtr eventPtr = default)
            where TState : struct
        {
            Change(control, ref state, updateType, eventPtr);
        }

        /// <summary>
        /// Perform one update of input state.
        /// </summary>
        /// <remarks>
        /// Incorporates the given state and triggers all state change monitors as needed.
        ///
        /// Note that input state changes performed with this method will not be visible on remotes as they will bypass
        /// event processing. It is effectively equivalent to directly writing into input state memory except that it
        /// also performs related tasks such as checking state change monitors, flipping buffers, or making the respective
        /// device current.
        /// </remarks>
        public static unsafe void Change<TState>(InputControl control, ref TState state, InputUpdateType updateType = default,
            InputEventPtr eventPtr = default)
            where TState : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (control.stateBlock.bitOffset != 0 || control.stateBlock.sizeInBits % 8 != 0)
                throw new ArgumentException($"Cannot change state of bitfield control '{control}' using this method", nameof(control));

            var device = control.device;
            var stateSize = Math.Min(UnsafeUtility.SizeOf<TState>(), control.m_StateBlock.alignedSizeInBytes);
            var statePtr = UnsafeUtility.AddressOf(ref state);
            var stateOffset = control.stateBlock.byteOffset - device.stateBlock.byteOffset;

            InputSystem.s_Manager.UpdateState(device,
                updateType != default ? updateType : InputSystem.s_Manager.defaultUpdateType, statePtr, stateOffset,
                (uint)stateSize,
                eventPtr.valid
                ? eventPtr.internalTime
                : InputRuntime.s_Instance.currentTime,
                eventPtr: eventPtr);
        }

        public static bool IsIntegerFormat(this FourCC format)
        {
            return format == InputStateBlock.FormatBit ||
                format == InputStateBlock.FormatInt ||
                format == InputStateBlock.FormatByte ||
                format == InputStateBlock.FormatShort ||
                format == InputStateBlock.FormatSBit ||
                format == InputStateBlock.FormatUInt ||
                format == InputStateBlock.FormatUShort ||
                format == InputStateBlock.FormatLong ||
                format == InputStateBlock.FormatULong;
        }

        /// <summary>
        /// Add a monitor that gets triggered every time the state of <paramref name="control"/> changes.
        /// </summary>
        /// <param name="control">A control sitting on an <see cref="InputDevice"/> that has been <see cref="InputDevice.added"/>.</param>
        /// <param name="monitor">Instance of the monitor that should be notified when state changes occur.</param>
        /// <param name="monitorIndex">Numeric index of the monitors. Monitors on a device are ordered by <em>decreasing</em> monitor index
        /// and invoked in that order.</param>
        /// <param name="groupIndex">Numeric group of the monitor. See remarks.</param>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c> -or- <paramref name="monitor"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The <see cref="InputDevice"/> of <paramref name="control"/> has not been <see cref="InputDevice.added"/>.</exception>
        /// <remarks>
        /// All monitors on an <see cref="InputDevice"/> are sorted by the complexity specified in their <paramref name="monitorIndex"/> (in decreasing order) and invoked
        /// in that order.
        ///
        /// Every handler gets an opportunity to set <see cref="InputEventPtr.handled"/> to <c>true</c>. When doing so, all remaining pending monitors
        /// from the same <paramref name="monitor"/> instance that have the same <paramref name="groupIndex"/> will be silenced and skipped over.
        /// This can be used to establish an order of event "consumption" where one change monitor may prevent another change monitor from triggering.
        ///
        /// Monitors are invoked <em>after</em> a state change has been written to the device. If, for example, a <see cref="StateEvent"/> is
        /// received that sets <see cref="Gamepad.leftTrigger"/> to <c>0.5</c>, the value is first applied to the control and then any state
        /// monitors that may be listening to the change are invoked (thus getting <c>0.5</c> if calling <see cref="InputControl{TValue}.ReadValue()"/>).
        ///
        /// <example>
        /// <code>
        /// class InputMonitor : IInputStateChangeMonitor
        /// {
        ///     public InputMonitor()
        ///     {
        ///         // Watch the left and right mouse button.
        ///         // By supplying monitor indices here, we not only receive the indices in NotifyControlStateChanged,
        ///         // we also create an ordering between the two monitors. The one on RMB will fire *before* the one
        ///         // on LMB in case there is a single event that changes both buttons.
        ///         InputState.AddChangeMonitor(Mouse.current.leftButton, this, monitorIndex: 1);
        ///         InputState.AddChangeMonitor(Mouse.current.rightButton, this, monitorIndex: 2);
        ///     }
        ///
        ///     public void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
        ///     {
        ///         Debug.Log($"{control} changed");
        ///
        ///         // We can add a monitor timeout that will trigger in case the state of the
        ///         // given control is not changed within the given time. Let's watch the control
        ///         // for 2 seconds. If nothing happens, we will get a call to NotifyTimerExpired.
        ///         // If, however, there is a state change, the timeout is automatically removed
        ///         // and we will see a call to NotifyControlStateChanged instead.
        ///         InputState.AddChangeMonitorTimeout(control, this, 2);
        ///     }
        ///
        ///     public void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
        ///     {
        ///         Debug.Log($"{control} was not changed within 2 seconds");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public static void AddChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1, uint groupIndex = default)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));
            if (!control.device.added)
                throw new ArgumentException($"Device for control '{control}' has not been added to system");

            InputSystem.s_Manager.AddStateChangeMonitor(control, monitor, monitorIndex, groupIndex);
        }

        public static IInputStateChangeMonitor AddChangeMonitor(InputControl control,
            NotifyControlValueChangeAction valueChangeCallback, int monitorIndex = -1,
            NotifyTimerExpiredAction timerExpiredCallback = null)
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
