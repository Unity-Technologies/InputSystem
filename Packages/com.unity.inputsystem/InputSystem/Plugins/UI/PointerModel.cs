using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    // A pointer is identified by a single unique integer ID. It has an associated position and the ability to press
    // on up to three buttons. It can also scroll.
    //
    // There's a single ExtendedPointerEventData instance allocated for the pointer which is used to retain the pointer's
    // event state. As part of that state is specific to button presses, each button retains a partial copy of press-specific
    // event information.
    //
    // A pointer can operate in 2D (mouse, pen, touch) or 3D (tracked). For 3D, screen-space 2D positions are derived
    // via raycasts based on world-space position and orientation.
    internal struct PointerModel
    {
        public bool changedThisFrame;
        public UIPointerType pointerType => eventData.pointerType;

        public Vector2 screenPosition
        {
            get => m_ScreenPosition;
            set
            {
                if (m_ScreenPosition != value)
                {
                    m_ScreenPosition = value;
                    changedThisFrame = true;
                }
            }
        }

        public Vector3 worldPosition
        {
            get => m_WorldPosition;
            set
            {
                if (m_WorldPosition != value)
                {
                    m_WorldPosition = value;
                    changedThisFrame = true;
                }
            }
        }

        public Quaternion worldOrientation
        {
            get => m_WorldOrientation;
            set
            {
                if (m_WorldOrientation != value)
                {
                    m_WorldOrientation = value;
                    changedThisFrame = true;
                }
            }
        }

        public Vector2 scrollDelta
        {
            get => m_ScrollDelta;
            set
            {
                if (m_ScrollDelta != value)
                {
                    changedThisFrame = true;
                    m_ScrollDelta = value;
                }
            }
        }

        public ButtonState leftButton;
        public ButtonState rightButton;
        public ButtonState middleButton;
        public ExtendedPointerEventData eventData;

        public PointerModel(int pointerId, int touchId, UIPointerType pointerType, InputDevice device, ExtendedPointerEventData eventData)
        {
            this.eventData = eventData;

            eventData.pointerId = pointerId;
            eventData.touchId = touchId;
            eventData.pointerType = pointerType;
            eventData.device = device;

            changedThisFrame = false;

            leftButton = default; leftButton.OnEndFrame();
            rightButton = default; rightButton.OnEndFrame();
            middleButton = default; middleButton.OnEndFrame();

            m_ScreenPosition = default;
            m_ScrollDelta = default;
            m_WorldOrientation = default;
            m_WorldPosition = default;
        }

        public void OnFrameFinished()
        {
            changedThisFrame = false;
            scrollDelta = default;
            leftButton.OnEndFrame();
            rightButton.OnEndFrame();
            middleButton.OnEndFrame();
        }

        private Vector2 m_ScreenPosition;
        private Vector2 m_ScrollDelta;
        private Vector3 m_WorldPosition;
        private Quaternion m_WorldOrientation;

        // State related to pressing and releasing individual bodies. Retains those parts of
        // PointerInputEvent that are specific to presses and releases.
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
            public bool wasReleasedThisFrame => m_FramePressState == PointerEventData.FramePressState.Released ||
            m_FramePressState == PointerEventData.FramePressState.PressedAndReleased;

            private RaycastResult pressRaycast;
            private GameObject pressObject;
            private GameObject rawPressObject;
            private GameObject lastPressObject;
            private GameObject dragObject;
            private Vector2 pressPosition;
            private float clickTime; // On Time.unscaledTime timeline, NOT input event time.
            private int clickCount;
            private bool dragging;

            public void CopyPressStateTo(PointerEventData eventData)
            {
                eventData.pointerPressRaycast = pressRaycast;
                eventData.pressPosition = pressPosition;
                eventData.clickCount = clickCount;
                eventData.clickTime = clickTime;
                // We can't set lastPress directly. Old input module uses three different event instances, one for each
                // button. We share one instance and just switch press states. Set pointerPress twice to get the lastPress
                // we need.
                //
                // NOTE: This does *NOT* quite work as stated in the docs. pointerPress is nulled out on button release which
                //       will set lastPress as a side-effect. This means that lastPress will only be non-null while no press is
                //       going on and will *NOT* refer to the last pressed object when a new object has been pressed on.
                eventData.pointerPress = lastPressObject;
                eventData.pointerPress = pressObject;
                eventData.rawPointerPress = rawPressObject;
                eventData.pointerDrag = dragObject;
                eventData.dragging = dragging;
            }

            public void CopyPressStateFrom(PointerEventData eventData)
            {
                pressRaycast = eventData.pointerPressRaycast;
                pressObject = eventData.pointerPress;
                rawPressObject = eventData.rawPointerPress;
                lastPressObject = eventData.lastPress;
                pressPosition = eventData.pressPosition;
                clickTime = eventData.clickTime;
                clickCount = eventData.clickCount;
                dragObject = eventData.pointerDrag;
                dragging = eventData.dragging;
            }

            public void OnEndFrame()
            {
                m_FramePressState = PointerEventData.FramePressState.NotChanged;
            }
        }
    }
}
