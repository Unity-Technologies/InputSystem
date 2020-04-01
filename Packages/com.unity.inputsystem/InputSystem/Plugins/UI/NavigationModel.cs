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
        public ButtonState submitButton;
        public ButtonState cancelButton;
        public AxisEventData eventData;

        public void Reset()
        {
            move = Vector2.zero;
            OnFrameFinished();
        }

        public void OnFrameFinished()
        {
            submitButton.OnFrameFinished();
            cancelButton.OnFrameFinished();
        }

        public struct ButtonState
        {
            private bool m_IsPressed;
            private PointerEventData.FramePressState m_FramePressState;

            public bool isPressed
            {
                get => m_IsPressed;
                set
                {
                    if (m_IsPressed != value)
                    {
                        m_IsPressed = value;

                        if (m_FramePressState == PointerEventData.FramePressState.NotChanged && value)
                            m_FramePressState = PointerEventData.FramePressState.Pressed;
                        else if (m_FramePressState == PointerEventData.FramePressState.NotChanged && !value)
                            m_FramePressState = PointerEventData.FramePressState.Released;
                        else if (m_FramePressState == PointerEventData.FramePressState.Pressed && !value)
                            m_FramePressState = PointerEventData.FramePressState.PressedAndReleased;
                    }
                }
            }

            public bool wasPressedThisFrame => m_FramePressState == PointerEventData.FramePressState.Pressed ||
            m_FramePressState == PointerEventData.FramePressState.PressedAndReleased;

            public void OnFrameFinished()
            {
                m_FramePressState = PointerEventData.FramePressState.NotChanged;
            }
        }
    }
}
#endif
