#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

////TODO: respect cursor lock mode

////TODO: investigate how driving the HW cursor behaves when FPS drops low
////      (also, maybe we can add support where we turn the gamepad mouse on and off automatically based on whether the system mouse is used)

////TODO: add support for acceleration

////TODO: automatically scale mouse speed to resolution such that it stays constant regardless of resolution

////TODO: make it work with PlayerInput such that it will automatically look up actions in the actual PlayerInput instance it is used with (based on the action IDs it has)

////REVIEW: should we default the SW cursor position to the center of the screen?

////REVIEW: consider this for inclusion directly in the input system

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// A component that creates a virtual <see cref="Mouse"/> device and drives its input from gamepad-style inputs. This effectively
    /// adds a software mouse cursor.
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
    ///
    /// Input from the component is visible in the same frame as the source input on its actions by virtue of using <see cref="InputState.Change"/>.
    /// </remarks>
    /// <seealso cref="Gamepad"/>
    /// <seealso cref="Mouse"/>
    [AddComponentMenu("Input/Virtual Mouse")]
    [HelpURL(InputSystem.kDocUrl + "/manual/UISupport.html#virtual-mouse-cursor-control")]
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
        /// Determines which cursor representation to use. If this is set to <see cref="CursorMode.SoftwareCursor"/>
        /// (the default), then <see cref="cursorGraphic"/> and <see cref="cursorTransform"/> define a software cursor
        /// that is made to correspond to the position of <see cref="virtualMouse"/>. If this is set to <see
        /// cref="CursorMode.HardwareCursorIfAvailable"/> and there is a native <see cref="Mouse"/> device present,
        /// the component will take over that mouse device and disable it (so as for it to not also generate position
        /// updates). It will then use <see cref="Mouse.WarpCursorPosition"/> to move the system mouse cursor to
        /// correspond to the position of the <see cref="virtualMouse"/>. In this case, <see cref="cursorGraphic"/>
        /// will be disabled and <see cref="cursorTransform"/> will not be updated.
        /// </summary>
        /// <value>Whether the system mouse cursor (if present) should be made to correspond with the virtual mouse position.</value>
        /// <remarks>
        /// Note that regardless of which mode is used for the cursor, mouse input is expected to be picked up from <see cref="virtualMouse"/>.
        ///
        /// Note that if <see cref="CursorMode.HardwareCursorIfAvailable"/> is used, the software cursor is still used
        /// if no native <see cref="Mouse"/> device is present.
        /// </remarks>
        public CursorMode cursorMode
        {
            get => m_CursorMode;
            set
            {
                if (m_CursorMode == value)
                    return;

                // If we're turning it off, make sure we re-enable the system mouse.
                if (m_CursorMode == CursorMode.HardwareCursorIfAvailable && m_SystemMouse != null)
                {
                    InputSystem.EnableDevice(m_SystemMouse);
                    m_SystemMouse = null;
                }

                m_CursorMode = value;

                if (m_CursorMode == CursorMode.HardwareCursorIfAvailable)
                    TryEnableHardwareCursor();
                else if (m_CursorGraphic != null)
                    m_CursorGraphic.enabled = true;
            }
        }

        /// <summary>
        /// The UI graphic element that represents the mouse cursor.
        /// </summary>
        /// <value>Graphic element for the software mouse cursor.</value>
        /// <remarks>
        /// If <see cref="cursorMode"/> is set to <see cref="CursorMode.HardwareCursorIfAvailable"/>, this graphic will
        /// be disabled.
        ///
        /// Also, this UI component implicitly determines the <c>Canvas</c> that defines the screen area for the cursor.
        /// The canvas that this graphic is on will be looked up using <c>GetComponentInParent</c> and then the <c>Canvas.pixelRect</c>
        /// of the canvas is used as the bounds for the cursor motion range.
        /// </remarks>
        /// <seealso cref="CursorMode.SoftwareCursor"/>
        public Graphic cursorGraphic
        {
            get => m_CursorGraphic;
            set
            {
                m_CursorGraphic = value;
                TryFindCanvas();
            }
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
        /// The Vector2 stick input that drives the mouse cursor, i.e. <see cref="Pointer.position"/> on
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
            // Hijack system mouse, if enabled.
            if (m_CursorMode == CursorMode.HardwareCursorIfAvailable)
                TryEnableHardwareCursor();

            // Add mouse device.
            if (m_VirtualMouse == null)
                m_VirtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
            else if (!m_VirtualMouse.added)
                InputSystem.AddDevice(m_VirtualMouse);

            // Set initial cursor position.
            if (m_CursorTransform != null)
            {
                var position = m_CursorTransform.anchoredPosition;
                InputState.Change(m_VirtualMouse.position, position);
                m_SystemMouse?.WarpCursorPosition(position);
            }

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

            // Let go of system mouse.
            if (m_SystemMouse != null)
            {
                InputSystem.EnableDevice(m_SystemMouse);
                m_SystemMouse = null;
            }

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

        private void TryFindCanvas()
        {
            m_Canvas = m_CursorGraphic?.GetComponentInParent<Canvas>();
        }

        private void TryEnableHardwareCursor()
        {
            var devices = InputSystem.devices;
            for (var i = 0; i < devices.Count; ++i)
            {
                var device = devices[i];
                if (device.native && device is Mouse mouse)
                {
                    m_SystemMouse = mouse;
                    break;
                }
            }

            if (m_SystemMouse == null)
            {
                if (m_CursorGraphic != null)
                    m_CursorGraphic.enabled = true;
                return;
            }

            InputSystem.DisableDevice(m_SystemMouse);

            // Sync position.
            if (m_VirtualMouse != null)
                m_SystemMouse.WarpCursorPosition(m_VirtualMouse.position.value);

            // Turn off mouse cursor image.
            if (m_CursorGraphic != null)
                m_CursorGraphic.enabled = false;
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
                // Motion has stopped.
                m_LastTime = default;
                m_LastStickValue = default;
            }
            else
            {
                var currentTime = InputState.currentTime;
                if (Mathf.Approximately(0, m_LastStickValue.x) && Mathf.Approximately(0, m_LastStickValue.y))
                {
                    // Motion has started.
                    m_LastTime = currentTime;
                }

                // Compute delta.
                var deltaTime = (float)(currentTime - m_LastTime);
                var delta = new Vector2(m_CursorSpeed * stickValue.x * deltaTime, m_CursorSpeed * stickValue.y * deltaTime);

                // Update position.
                var currentPosition = m_VirtualMouse.position.value;
                var newPosition = currentPosition + delta;

                ////REVIEW: for the hardware cursor, clamp to something else?
                // Clamp to canvas.
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

                // Update software cursor transform, if any.
                if (m_CursorTransform != null &&
                    (m_CursorMode == CursorMode.SoftwareCursor ||
                     (m_CursorMode == CursorMode.HardwareCursorIfAvailable && m_SystemMouse == null)))
                    m_CursorTransform.anchoredPosition = newPosition;

                m_LastStickValue = stickValue;
                m_LastTime = currentTime;

                // Update hardware cursor.
                m_SystemMouse?.WarpCursorPosition(newPosition);
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

        [Header("Cursor")]
        [Tooltip("Whether the component should set the cursor position of the hardware mouse cursor, if one is available. If so, "
            + "the software cursor pointed (to by 'Cursor Graphic') will be hidden.")]
        [SerializeField] private CursorMode m_CursorMode;
        [Tooltip("The graphic that represents the software cursor. This is hidden if a hardware cursor (see 'Cursor Mode') is used.")]
        [SerializeField] private Graphic m_CursorGraphic;
        [Tooltip("The transform for the software cursor. Will only be set if a software cursor is used (see 'Cursor Mode'). Moving the cursor "
            + "updates the anchored position of the transform.")]
        [SerializeField] private RectTransform m_CursorTransform;

        [Header("Motion")]
        [Tooltip("Speed in pixels per second with which to move the cursor. Scaled by the input from 'Stick Action'.")]
        [SerializeField] private float m_CursorSpeed = 400;
        [Tooltip("Scale factor to apply to 'Scroll Wheel Action' when setting the mouse 'scrollWheel' control.")]
        [SerializeField] private float m_ScrollSpeed = 45;

        [Space(10)]
        [Tooltip("Vector2 action that moves the cursor left/right (X) and up/down (Y) on screen.")]
        [SerializeField] private InputActionProperty m_StickAction;
        [Tooltip("Button action that triggers a left-click on the mouse.")]
        [SerializeField] private InputActionProperty m_LeftButtonAction;
        [Tooltip("Button action that triggers a middle-click on the mouse.")]
        [SerializeField] private InputActionProperty m_MiddleButtonAction;
        [Tooltip("Button action that triggers a right-click on the mouse.")]
        [SerializeField] private InputActionProperty m_RightButtonAction;
        [Tooltip("Button action that triggers a forward button (button #4) click on the mouse.")]
        [SerializeField] private InputActionProperty m_ForwardButtonAction;
        [Tooltip("Button action that triggers a back button (button #5) click on the mouse.")]
        [SerializeField] private InputActionProperty m_BackButtonAction;
        [Tooltip("Vector2 action that feeds into the mouse 'scrollWheel' action (scaled by 'Scroll Speed').")]
        [SerializeField] private InputActionProperty m_ScrollWheelAction;

        private Canvas m_Canvas; // Canvas that gives the motion range for the software cursor.
        private Mouse m_VirtualMouse;
        private Mouse m_SystemMouse;
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

        /// <summary>
        /// Determines how the cursor for the virtual mouse is represented.
        /// </summary>
        /// <seealso cref="cursorMode"/>
        public enum CursorMode
        {
            /// <summary>
            /// The cursor is represented as a UI element. See <see cref="cursorGraphic"/>.
            /// </summary>
            SoftwareCursor,

            /// <summary>
            /// If a native <see cref="Mouse"/> device is present, its cursor will be used and driven
            /// by the virtual mouse using <see cref="Mouse.WarpCursorPosition"/>. The software cursor
            /// referenced by <see cref="cursorGraphic"/> will be disabled.
            ///
            /// Note that if no native <see cref="Mouse"/> is present, behavior will fall back to
            /// <see cref="SoftwareCursor"/>.
            /// </summary>
            HardwareCursorIfAvailable,
        }
    }
}
#endif // PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
