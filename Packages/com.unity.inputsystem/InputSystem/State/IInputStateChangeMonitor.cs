namespace UnityEngine.Experimental.Input.LowLevel
{
    public interface IInputStateChangeMonitor
    {
        void NotifyControlValueChanged(InputControl control, double time, int monitorIndex);
        void NotifyTimerExpired(InputControl control, double time, int monitorIndex, int timerIndex);
    }
}
