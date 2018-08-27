////REVIEW: rename to IInputStateChangeMonitor

namespace UnityEngine.Experimental.Input.LowLevel
{
    public interface IInputStateChangeMonitor
    {
        void NotifyControlValueChanged(InputControl control, double time, long monitorIndex);
        void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex);
    }
}
