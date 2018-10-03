////REVIEW: could have a monitor path where if there's multiple state monitors on the same control with
////        the same listener, the monitor is notified only once but made aware of the multiple triggers

namespace UnityEngine.Experimental.Input.LowLevel
{
    public interface IInputStateChangeMonitor
    {
        /// <summary>
        /// Called when the state monitored by a state change monitor has been modified.
        /// </summary>
        /// <param name="control">Control that is being monitored by the state change monitor and that had its state
        /// memory changed.</param>
        /// <param name="time"></param>
        /// <param name="eventPtr">If the state change was initiated by a state event, this is the pointer to the event.
        /// Otherwise it is null.</param>
        /// <param name="monitorIndex"></param>
        void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex);

        /// <summary>
        /// Called when a timeout set on a state change monitor has expired.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="time"></param>
        /// <param name="monitorIndex"></param>
        /// <param name="timerIndex"></param>
        /// <seealso cref="InputSystem.AddStateChangeMonitorTimeout"/>
        void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex);
    }
}
