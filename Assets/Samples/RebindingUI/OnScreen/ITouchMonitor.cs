using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    internal interface ITouchMonitor : IInputStateChangeMonitor
    {
        void Reset(Rect bounds, AreaShape shape, Detector.GestureEventHandler handler);
    }
}
