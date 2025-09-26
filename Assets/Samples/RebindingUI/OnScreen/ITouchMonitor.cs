using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    interface ITouchMonitor : IInputStateChangeMonitor
    {
        void Reset(Rect bounds, AreaShape shape, ChangeMonitor.GestureEventHandler handler);
    }
}
