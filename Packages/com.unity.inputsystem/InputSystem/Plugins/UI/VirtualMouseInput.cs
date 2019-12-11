using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;

////TODO: add support for acceleration

////TODO: automatically scale mouse speed to resolution such that it stays constant regardless of resolution

////TODO: make it work with PlayerInput such that it will automatically look up actions in the actual PlayerInput instance it is used with (based on the action IDs it has)

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// A component that creates a virtual <see cref="Mouse"/> device and drives its input from gamepad-style inputs.
    /// </summary>
    /// <remarks>
    /// This component can be used with UIs that are designed for mouse input, i.e. need to be operated with a cursor.
    /// By hooking up the <see cref="InputAction"/>s of this component to gamepad input and directing <see cref="cursorTransform"/>
    /// to the UI transform of the cursor, you can use this component to drive an on-screen cursor.
    ///
    /// Note that this component does not actually trigger UI input itself. Instead, it creates a virtual <see cref="Mouse"/>
    /// device which can then be picked up elsewhere (such as by <see cref="InputSystemUIInputModule"/>) where mouse/pointer input
    /// is expected.
    ///
    /// Also note that if there is a <see cref="Mouse"/> added by the platform, it is not impacted by this component. More specifically,
    /// the system mouse cursor will not be moved or otherwise used by this component.
    /// </remarks>
    /// <seealso cref="Gamepad"/>
    /// <seealso cref="Mouse"/>
    [AddComponentMenu("Input/Virtual Mouse")]
    public class VirtualMouseInput : MonoBehaviour
    {
        /// <summary>
        /// Optional transform that will be updated to correspond to the current mouse position.
        /// </summary>
        /// <value>Transform to update with mouse position.</value>
        /// <remarks>
        /// This is useful for having a UI object that directly represents the mouse cursor. Simply add both the
        /// <c>VirtualMouseInput</c> component and an <a href="https://docs.unity3d.com/Manual/script-Image.html">Image</a>
        /// component and hook the <a href="https://docs.unity3d.com/ScriptReference/RectTransform.html">RectTransform</a>
        /// component for the UI object into here. The object as a whole will then follow the generated mouse cursor
        /// motion.
        /// </remarks>
        public RectTransform cursorTransform
        {
            get => m_CursorTransform;
            set => m_CursorTransform = value;
        }

        /// <summary>
        /// The canvas that defines the screen area for the mouse cursor. Optional.
        /// </summary>
        /// <value>Screen for the mouse cursor.</value>
        /// <remarks>
        /// If this is set, the cursor will automatically be clamped to the <a
        /// href="https://docs.unity3d.com/ScriptReference/Canvas-pixelRect.html">pixelRect</a>
        /// of the canvas.
        /// </remarks>
        public Canvas canvas
        {
            get => m_Canvas;
            set => m_Canvas = value;////TODO: clamp current position
        }

        /// <summary>
        /// How many pixels per second the cursor travels in one axis when the respective axis from
        /// <see cref="stickAction"/> is 1.
        /// </summary>
        /// <value>Mouse speed in pixels per second.</value>
        public float cursorSpeed
        {
            get => m_CursorSpeed;
            set => m_CursorSpeed = value;
        }

        /// <summary>
        /// Multiplier for values received from <see cref="scrollWheelAction"/>.
        /// </summary>
        /// <value>Multiplier for scroll values.</value>
        public float scrollSpeed
        {
            get => m_ScrollSpeed;
            set => m_ScrollSpeed = value;
        }

        /*
        /// <summary>
        /// How many seconds it takes for the cursor to reach full <see cref="cursorSpeed"/>. By default,
        /// this is 0 meaning that there is no acceleration and the cursor will always travel at maximum
        /// speed.
        /// </summary>
        /// <value>Mouse acceleration time in seconds.</value>
        /// <remarks>
        /// To compute current mouse speed while the cursor is in an acceleration phase, <a
        /// href="https://docs.unity3d.com/ScriptReference/Mathf.SmoothDamp.html">Mathf.SmoothDamp</a> is used.
        /// </remarks>
        public float mouseAcceleration
        {
            get => m_MouseAcceleration;
            set => m_MouseAcceleration = value;
        }
        */

        /// <summary>
        /// The virtual mouse device that the component feeds with input.
        /// </summary>
        /// <value>Instance of virtual mouse or <c>null</c>.</value>
        /// <remarks>
        /// This is only initialized after the component has been enabled for the first time. Note that
        /// when subsequently disabling the component, the property will continue to return the mouse device
        /// but the device will not be added to the system while the component is not enabled.
        /// </remarks>
        public Mouse virtualMouse => m_VirtualMouse;

        /// <summary>
        /// The Vector2 stick input that drives the mouse cursor, i.e. <see cref="Mouse.position"/> on
        /// <see cref="virtualMouse"/> and the <a
        /// href="https://docs.unity3d.com/ScriptReference/RectTransform-anchoredPosition.html">anchoredPosition</a>
        /// on <see cref="cursorTransform"/> (if set).
        /// </summary>
        /// <value>Stick input that drives cursor position.</value>
        /// <remarks>
        /// This should normally be bound to controls such as <see cref="Gamepad.leftStick"/> and/or
        /// <see cref="Gamepad.rightStick"/>.
        /// </remarks>
        public InputActionProperty stickAction
        {
            get => m_StickAction;
            set => SetAction(ref m_StickAction, value);
        }

        /// <summary>
        /// Optional button input that determines when <see cref="Mouse.leftButton"/> is pressed on
        /// <see cref="virtualMouse"/>.
        /// </summary>
        /// <value>Input for <see cref="Mouse.leftButton"/>.</value>
        public InputActionProperty leftButtonAction
        {
            get => m_LeftButtonAction;
            set
            {
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_LeftButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetAction(ref m_LeftButtonAction, value);
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_LeftButtonAction, m_ButtonActionTriggeredDelegate, true);
            }
        }

        /// <summary>
        /// Optional button input that determines when <see cref="Mouse.rightButton"/> is pressed on
        /// <see cref="virtualMouse"/>.
        /// </summary>
        /// <value>Input for <see cref="Mouse.rightButton"/>.</value>
        public InputActionProperty rightButtonAction
        {
            get => m_RightButtonAction;
            set
            {
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_RightButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetAction(ref m_RightButtonAction, value);
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_RightButtonAction, m_ButtonActionTriggeredDelegate, true);
            }
        }

        /// <summary>
        /// Optional button input that determines when <see cref="Mouse.middleButton"/> is pressed on
        /// <see cref="virtualMouse"/>.
        /// </summary>
        /// <value>Input for <see cref="Mouse.middleButton"/>.</value>
        public InputActionProperty middleButtonAction
        {
            get => m_MiddleButtonAction;
            set
            {
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetAction(ref m_MiddleButtonAction, value);
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, true);
            }
        }

        /// <summary>
        /// Optional button input that determines when <see cref="Mouse.forwardButton"/> is pressed on
        /// <see cref="virtualMouse"/>.
        /// </summary>
        /// <value>Input for <see cref="Mouse.forwardButton"/>.</value>
        public InputActionProperty forwardButtonAction
        {
            get => m_ForwardButtonAction;
            set
            {
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_ForwardButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetAction(ref m_ForwardButtonAction, value);
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_ForwardButtonAction, m_ButtonActionTriggeredDelegate, true);
            }
        }

        /// <summary>
        /// Optional button input that determines when <see cref="Mouse.forwardButton"/> is pressed on
        /// <see cref="virtualMouse"/>.
        /// </summary>
        /// <value>Input for <see cref="Mouse.forwardButton"/>.</value>
        public InputActionProperty backButtonAction
        {
            get => m_BackButtonAction;
            set
            {
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_BackButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetAction(ref m_BackButtonAction, value);
                if (m_ButtonActionTriggeredDelegate != null)
                    SetActionCallback(m_BackButtonAction, m_ButtonActionTriggeredDelegate, true);
            }
        }

        /// <summary>
        /// Optional Vector2 value input that determines the value of <see cref="Mouse.scroll"/> on
        /// <see cref="virtualMouse"/>.
        /// </summary>
        /// <value>Input for <see cref="Mouse.scroll"/>.</value>
        /// <remarks>
        /// In case you want to only bind vertical scrolling, simply have a <see cref="Composites.Vector2Composite"/>
        /// with only <c>Up</c> and <c>Down</c> bound and <c>Left</c> and <c>Right</c> deleted or bound to nothing.
        /// </remarks>
        public InputActionProperty scrollWheelAction
        {
            get => m_ScrollWheelAction;
            set => SetAction(ref m_ScrollWheelAction, value);
        }

        protected void OnEnable()
        {
            // Add mouse device.
            if (m_VirtualMouse == null)
                m_VirtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
            else if (!m_VirtualMouse.added)
                InputSystem.AddDevice(m_VirtualMouse);

            // Set initial cursor position.
            if (m_CursorTransform != null)
                InputState.Change(m_VirtualMouse.position, m_CursorTransform.anchoredPosition);

            // Hook into input update.
            if (m_AfterInputUpdateDelegate == null)
                m_AfterInputUpdateDelegate = OnAfterInputUpdate;
            InputSystem.onAfterUpdate += m_AfterInputUpdateDelegate;

            // Hook into actions.
            if (m_ButtonActionTriggeredDelegate == null)
                m_ButtonActionTriggeredDelegate = OnButtonActionTriggered;
            SetActionCallback(m_LeftButtonAction, m_ButtonActionTriggeredDelegate, true);
            SetActionCallback(m_RightButtonAction, m_ButtonActionTriggeredDelegate, true);
            SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, true);
            SetActionCallback(m_ForwardButtonAction, m_ButtonActionTriggeredDelegate, true);
            SetActionCallback(m_BackButtonAction, m_ButtonActionTriggeredDelegate, true);

            // Enable actions.
            m_StickAction.action?.Enable();
            m_LeftButtonAction.action?.Enable();
            m_RightButtonAction.action?.Enable();
            m_MiddleButtonAction.action?.Enable();
            m_ForwardButtonAction.action?.Enable();
            m_BackButtonAction.action?.Enable();
            m_ScrollWheelAction.action?.Enable();
        }

        protected void OnDisable()
        {
            // Remove mouse device.
            if (m_VirtualMouse != null && m_VirtualMouse.added)
                InputSystem.RemoveDevice(m_VirtualMouse);

            // Remove ourselves from input update.
            if (m_AfterInputUpdateDelegate != null)
                InputSystem.onAfterUpdate -= m_AfterInputUpdateDelegate;

            // Disable actions.
            m_StickAction.action?.Disable();
            m_LeftButtonAction.action?.Disable();
            m_RightButtonAction.action?.Disable();
            m_MiddleButtonAction.action?.Disable();
            m_ForwardButtonAction.action?.Disable();
            m_BackButtonAction.action?.Disable();
            m_ScrollWheelAction.action?.Disable();

            // Unhock from actions.
            if (m_ButtonActionTriggeredDelegate != null)
            {
                SetActionCallback(m_LeftButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetActionCallback(m_RightButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetActionCallback(m_ForwardButtonAction, m_ButtonActionTriggeredDelegate, false);
                SetActionCallback(m_BackButtonAction, m_ButtonActionTriggeredDelegate, false);
            }

            m_LastTime = default;
            m_LastStickValue = default;
        }

        private void UpdateMotion()
        {
            if (m_VirtualMouse == null)
                return;

            // Read current stick value.
            var stickAction = m_StickAction.action;
            if (stickAction == null)
                return;
            var stickValue = stickAction.ReadValue<Vector2>();
            if (Mathf.Approximately(0, stickValue.x) && Mathf.Approximately(0, stickValue.y))
            {
                ////REVIEW: should mouse acceleration also be applied as deceleration?
                // Motion has stopped.
                m_LastTime = default;
                m_LastStickValue = default;
            }
            else
            {
                var currentTime = InputRuntime.s_Instance.currentTime;
                if (Mathf.Approximately(0, m_LastStickValue.x) && Mathf.Approximately(0, m_LastStickValue.y))
                {
                    // Motion has started.
                    m_LastTime = currentTime;
                }

                // Compute delta.
                var deltaTime = (float)(currentTime - m_LastTime);
                var delta = new Vector2(m_CursorSpeed * stickValue.x * deltaTime, m_CursorSpeed * stickValue.y * deltaTime);

                // Update position.
                var currentPosition = m_VirtualMouse.position.ReadValue();
                var newPosition = currentPosition + delta;

                if (m_Canvas != null)
                {
                    // Clamp to canvas.
                    var pixelRect = m_Canvas.pixelRect;
                    newPosition.x = Mathf.Clamp(newPosition.x, pixelRect.xMin, pixelRect.xMax);
                    newPosition.y = Mathf.Clamp(newPosition.y, pixelRect.yMin, pixelRect.yMax);
                }

                ////REVIEW: the fact we have no events on these means that actions won't have an event ID to go by; problem?
                InputState.Change(m_VirtualMouse.position, newPosition);
                InputState.Change(m_VirtualMouse.delta, delta);

                // Update transform, if any.
                if (m_CursorTransform != null)
                    m_CursorTransform.anchoredPosition = newPosition;

                m_LastStickValue = stickValue;
                m_LastTime = currentTime;
            }

            // Update scroll wheel.
            var scrollAction = m_ScrollWheelAction.action;
            if (scrollAction != null)
            {
                var scrollValue = scrollAction.ReadValue<Vector2>();
                scrollValue.x *= m_ScrollSpeed;
                scrollValue.y *= m_ScrollSpeed;

                InputState.Change(m_VirtualMouse.scroll, scrollValue);
            }
        }

        [SerializeField] private float m_CursorSpeed = 400;
        [SerializeField] private float m_ScrollSpeed = 45;
        [SerializeField] private RectTransform m_CursorTransform;
        [SerializeField] private Canvas m_Canvas;
        [SerializeField] private InputActionProperty m_StickAction;
        [SerializeField] private InputActionProperty m_LeftButtonAction;
        [SerializeField] private InputActionProperty m_MiddleButtonAction;
        [SerializeField] private InputActionProperty m_RightButtonAction;
        [SerializeField] private InputActionProperty m_ForwardButtonAction;
        [SerializeField] private InputActionProperty m_BackButtonAction;
        [SerializeField] private InputActionProperty m_ScrollWheelAction;

        private Mouse m_VirtualMouse;
        private Action m_AfterInputUpdateDelegate;
        private Action<InputAction.CallbackContext> m_ButtonActionTriggeredDelegate;
        private double m_LastTime;
        private Vector2 m_LastStickValue;

        private void OnButtonActionTriggered(InputAction.CallbackContext context)
        {
            if (m_VirtualMouse == null)
                return;

            // The button controls are bit controls. We can't (yet?) use InputState.Change to state
            // the change of those controls as the state update machinery of InputManager only supports
            // byte region updates. So we just grab the full state of our virtual mouse, then update
            // the button in there and then simply overwrite the entire state.

            var action = context.action;
            MouseButton? button = null;
            if (action == m_LeftButtonAction.action)
                button = MouseButton.Left;
            else if (action == m_RightButtonAction.action)
                button = MouseButton.Right;
            else if (action == m_MiddleButtonAction.action)
                button = MouseButton.Middle;
            else if (action == m_ForwardButtonAction.action)
                button = MouseButton.Forward;
            else if (action == m_BackButtonAction.action)
                button = MouseButton.Back;

            if (button != null)
            {
                var isPressed = context.control.IsPressed();
                m_VirtualMouse.CopyState<MouseState>(out var mouseState);
                mouseState.WithButton(button.Value, isPressed);

                InputState.Change(m_VirtualMouse, mouseState);
            }
        }

        private static void SetActionCallback(InputActionProperty field, Action<InputAction.CallbackContext> callback, bool install = true)
        {
            var action = field.action;
            if (action == null)
                return;

            // We don't need the performed callback as our mouse buttons are binary and thus
            // we only care about started (1) and canceled (0).

            if (install)
            {
                action.started += callback;
                action.canceled += callback;
            }
            else
            {
                action.started -= callback;
                action.canceled -= callback;
            }
        }

        private static void SetAction(ref InputActionProperty field, InputActionProperty value)
        {
            var oldValue = field;
            field = value;

            if (oldValue.reference == null)
            {
                var oldAction = oldValue.action;
                if (oldAction != null && oldAction.enabled)
                {
                    oldAction.Disable();
                    if (value.reference == null)
                        value.action?.Enable();
                }
            }
        }

        private void OnAfterInputUpdate()
        {
            UpdateMotion();
        }
    }
}
