#if UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    internal struct NavigationModel
    {
        public Vector2 move;
        public int consecutiveMoveCount;
        public MoveDirection lastMoveDirection;
        public float lastMoveTime;
        public AxisEventData eventData;
        public InputDevice device;
        public bool wasSubmitButtonReleased;
        public bool wasCancelButtonReleased;

        public void Reset()
        {
            move = Vector2.zero;
            wasCancelButtonReleased = false;
            wasSubmitButtonReleased = false;
        }
    }

    internal struct SubmitCancelModel
    {
        public BaseEventData eventData;
        public InputDevice device;
    }
}
#endif
