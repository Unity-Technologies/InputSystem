using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    internal interface ITouchProcessor
    {
        void OnTouchBegin(Detector context, in TouchState[] touches, int count, int index);
        void OnTouchEnd(Detector context, in TouchState[] touches, int count, int index);
        void OnTouchMoved(Detector context, in TouchState[] touches, int count, int index);
        void OnTouchCanceled(Detector context, in TouchState[] touches, int count, int index);
    }
}
