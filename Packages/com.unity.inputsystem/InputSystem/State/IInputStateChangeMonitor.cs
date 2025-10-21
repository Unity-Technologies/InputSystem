////REVIEW: could have a monitor path where if there's multiple state monitors on the same control with
////        the same listener, the monitor is notified only once but made aware of the multiple triggers

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface used to monitor input state changes.
    /// </summary>
    /// <remarks>
    /// Use <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/> to install a state change monitor receiving state change
    /// callbacks for a specific control.
    /// </remarks>
    /// <seealso cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/>
    public interface IInputStateChangeMonitor
    {
        ////REVIEW: For v2, consider changing the signature of this to put the "was consumed" signal *outside* the eventPtr
        /// <summary>
        /// Called when the state monitored by a state change monitor has been modified.
        /// </summary>
        /// <param name="control">Control that is being monitored by the state change monitor and that had its state
        /// memory changed.</param>
        /// <param name="time">Time on the <see cref="InputEvent.time"/> timeline at which the control state change was received.</param>
        /// <param name="eventPtr">If the state change was initiated by a state event (either a <see cref="StateEvent"/>
        /// or <see cref="DeltaStateEvent"/>), this is the pointer to that event. Otherwise it is pointer that is still
        /// <see cref="InputEventPtr.valid"/>, but refers a "dummy" event that is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <param name="monitorIndex">Index of the monitor as passed to <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/>.
        /// </param>
        /// <remarks>
        /// To signal that the state change has been processed by the monitor and that no other pending notifications on the
        /// same monitor instance should be sent, set the <see cref="InputEventPtr.handled"/> flag to <c>true</c> on <paramref name="eventPtr"/>.
        /// Note, however, that aside from only silencing change monitors on the same <see cref="IInputStateChangeMonitor"/> instance,
        /// it also only silences change monitors with the same <c>groupIndex</c> value as supplied to
        /// <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/>.
        /// </remarks>
        void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex);

        /// <summary>
        /// Called when a timeout set on a state change monitor has expired.
        /// </summary>
        /// <param name="control">Control on which the timeout expired.</param>
        /// <param name="time">Input time at which the timer expired. This is the time at which an <see cref="InputSystem.Update"/> is being
        /// run whose <see cref="InputState.currentTime"/> is past the time of expiration.</param>
        /// <param name="monitorIndex">Index of the monitor as given to <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/>.</param>
        /// <param name="timerIndex">Index of the timer as given to <see cref="InputState.AddChangeMonitorTimeout"/>.</param>
        /// <seealso cref="InputState.AddChangeMonitorTimeout"/>
        void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex);
    }
}
