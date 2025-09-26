using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    interface ITouchProcessor
    {
        void OnTouchBegin(ChangeMonitor context, in TouchState[] touches, int count, int index);
        void OnTouchEnd(ChangeMonitor context, in TouchState[] touches, int count, int index);
        void OnTouchMoved(ChangeMonitor context, in TouchState[] touches, int count, int index);
        void OnTouchCanceled(ChangeMonitor context, in TouchState[] touches, int count, int index);
    }
}
