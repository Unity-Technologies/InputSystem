using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

////TODO: add ability to query which device was last used with any of the actions

////TODO: come up with an action response system that doesn't require hooking and unhooking all those delegates

////TODO: touch vs mouse will need refinement in both the action and the device stuff

namespace UnityEngine.Experimental.Input.Plugins.UI
{
    /// <summary>
    /// Input module that takes its input from <see cref="InputAction">input actions</see>.
    /// </summary>
    /// <remarks>
    /// This UI input module has the advantage over other such modules that it doesn't have to know
    /// what devices and types of devices input is coming from. Instead, the actions hide the actual
    /// sources of input from the module.
    /// </remarks>
    public class UIActionInputModule : UIInputModule
    {
        private static void SwapAction(ref InputActionProperty oldProperty, InputActionProperty newProperty, bool actionsHooked, Action<InputAction.CallbackContext> actionCallback)
        {
            if (oldProperty != null)
            {
                if (actionsHooked)
                    oldProperty.action.performed -= actionCallback;
            }

            oldProperty = newProperty;

            if (oldProperty != null)
            {
                if (actionsHooked)
                    oldProperty.action.performed += actionCallback;
            }
        }

        [Serializable]
        private struct TouchResponder
        {
            public TouchResponder(int pointerId, InputActionProperty position, InputActionProperty phase)
            {
                actionCallback = null;
                m_ActionsHooked = false;
                state = new TouchModel(pointerId);
                m_Position = position;
                m_Phase = phase;
            }

            [NonSerialized]
            public TouchModel state;

            [NonSerialized]
            public Action<InputAction.CallbackContext> actionCallback;

            public InputActionProperty position
            {
                get { return m_Position; }
                set { SwapAction(ref m_Position, value, m_ActionsHooked, actionCallback); }
            }

            public InputActionProperty phase
            {
                get { return m_Phase; }
                set { SwapAction(ref m_Phase, value, m_ActionsHooked, actionCallback); }
            }

            public bool actionsHooked
            {
                get { return m_ActionsHooked; }
            }

            public void HookActions()
            {
                if (!m_ActionsHooked)
                {
                    m_ActionsHooked = true;

                    var positionAction = m_Position.action;
                    if (positionAction != null)
                        positionAction.performed += actionCallback;

                    var phaseAction = m_Phase.action;
                    if (phaseAction != null)
                        phaseAction.performed += actionCallback;
                }
            }

            public void UnhookActions()
            {
                if (m_ActionsHooked)
                {
                    if (!m_ActionsHooked)
                    {
                        m_ActionsHooked = false;

                        var positionAction = m_Position.action;
                        if (positionAction != null)
                            positionAction.performed -= actionCallback;

                        var phaseAction = m_Phase.action;
                        if (phaseAction != null)
                            phaseAction.performed -= actionCallback;
                    }
                }
            }

            private bool m_ActionsHooked;

            [SerializeField]
            private InputActionProperty m_Position;
            [SerializeField]
            private InputActionProperty m_Phase;
        }

        [Serializable]
        private struct TrackedDeviceResponder
        {
            public TrackedDeviceResponder(int pointerId, InputActionProperty position, InputActionProperty orientation, InputActionProperty select)
            {
                actionCallback = null;
                m_ActionsHooked = false;
                state = new TrackedDeviceModel(pointerId);
                m_Position = position;
                m_Orientation = orientation;
                m_Select = select;
            }

            [NonSerialized]
            public TrackedDeviceModel state;

            [NonSerialized]
            public Action<InputAction.CallbackContext> actionCallback;

            public InputActionProperty position
            {
                get { return m_Position; }
                set { SwapAction(ref m_Position, value, m_ActionsHooked, actionCallback); }
            }

            public InputActionProperty orientation
            {
                get { return m_Orientation; }
                set { SwapAction(ref m_Orientation, value, m_ActionsHooked, actionCallback); }
            }

            public InputActionProperty select
            {
                get { return m_Select; }
                set { SwapAction(ref m_Select, value, m_ActionsHooked, actionCallback); }
            }

            public bool actionsHooked
            {
                get { return m_ActionsHooked; }
            }

            public void HookActions()
            {
                if (!m_ActionsHooked)
                {
                    m_ActionsHooked = true;

                    var positionAction = m_Position.action;
                    if (positionAction != null)
                        positionAction.performed += actionCallback;

                    var orientationAction = m_Orientation.action;
                    if (orientationAction != null)
                        orientationAction.performed += actionCallback;

                    var selectAction = m_Select.action;
                    if (selectAction != null)
                        selectAction.performed += actionCallback;
                }
            }

            public void UnhookActions()
            {
                if (m_ActionsHooked)
                {
                    if (!m_ActionsHooked)
                    {
                        m_ActionsHooked = false;

                        var positionAction = m_Position.action;
                        if (positionAction != null)
                            positionAction.performed -= actionCallback;

                        var orientationAction = m_Orientation.action;
                        if (orientationAction != null)
                            orientationAction.performed -= actionCallback;

                        var selectAction = m_Orientation.action;
                        if (selectAction != null)
                            selectAction.performed -= actionCallback;
                    }
                }
            }

            private bool m_ActionsHooked;

            [SerializeField]
            private InputActionProperty m_Position;
            [SerializeField]
            private InputActionProperty m_Orientation;
            [SerializeField]
            private InputActionProperty m_Select;
        }

        /// <summary>
        /// This enables background event processing, so that the Input Module can continue sending UI events even in the background.
        /// </summary>
        public bool sendEventsWhenInBackground
        {
            get { return m_SendEventsWhenInBackground; }
            set { m_SendEventsWhenInBackground = value; }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D screen position.
        /// </see> used as a cursor for pointing at UI elements.
        /// </summary>
        public InputActionProperty point
        {
            get { return m_PointAction; }
            set { SwapAction(ref m_PointAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector.
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        public InputActionProperty move
        {
            get { return m_MoveAction; }
            set { SwapAction(ref m_MoveAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2"> scroll wheel value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionProperty scrollWheel
        {
            get { return m_ScrollWheelAction; }
            set { SwapAction(ref m_ScrollWheelAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionProperty leftClick
        {
            get { return m_LeftClickAction; }
            set { SwapAction(ref m_LeftClickAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionProperty middleClick
        {
            get { return m_MiddleClickAction; }
            set { SwapAction(ref m_MiddleClickAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionProperty rightClick
        {
            get { return m_RightClickAction; }
            set { SwapAction(ref m_RightClickAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="BaseEventData"/> events.
        /// </summary>
        public InputActionProperty submit
        {
            get { return m_SubmitAction; }
            set { SwapAction(ref m_SubmitAction, value, m_ActionsHooked, OnAction); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="BaseEventData"/> events.
        /// </summary>
        public InputActionProperty cancel
        {
            get { return m_CancelAction; }
            set { SwapAction(ref m_CancelAction, value, m_ActionsHooked, OnAction); }
        }

        protected override void Awake()
        {
            base.Awake();

            m_RollingPointerId = 0;
            mouseState = new MouseModel(m_RollingPointerId++);
            joystickState.Reset();

            if (m_Touches == null)
                m_Touches = new List<TouchResponder>();

            for (int i = 0; i < m_Touches.Count; i++)
            {
                var responder = m_Touches[i];
                responder.state = new TouchModel(m_RollingPointerId++);

                var newIndex = i;
                responder.actionCallback = delegate(InputAction.CallbackContext context)
                {
                    OnTouchAction(newIndex, context);
                };
                m_Touches[i] = responder;
            }

            if (m_TrackedDevices == null)
                m_TrackedDevices = new List<TrackedDeviceResponder>();

            for (int i = 0; i < m_TrackedDevices.Count; i++)
            {
                var responder = m_TrackedDevices[i];
                responder.state = new TrackedDeviceModel(m_RollingPointerId++);

                var newIndex = i;
                responder.actionCallback = delegate(InputAction.CallbackContext context)
                {
                    OnTrackedDeviceAction(newIndex, context);
                };
                m_TrackedDevices[i] = responder;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnhookActions();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            HookActions();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            UnhookActions();
        }

        private bool ShouldIgnoreEventsOnNoFocus()
        {
            if (m_SendEventsWhenInBackground)
                return true;

            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
#if UNITY_EDITOR
                    if (UnityEditor.EditorApplication.isRemoteConnected)
                        return false;
#endif
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Adds Touch UI responses based the Actions provided.
        /// </summary>
        /// <param name="position">A <see cref="Vector2"/> screen space value that represents the position of the touch.</param>
        /// <param name="phase">A <see cref="PointerPhase"/> value that represents the current state of the touch event.</param>
        /// <returns>The Pointer Id that represents UI events from this Touch action set.</returns>
        public int AddTouch(InputActionProperty position, InputActionProperty phase)
        {
            var id = m_RollingPointerId++;

            var newResponder = new TouchResponder(id, position, phase);

            var index = m_Touches.Count;
            newResponder.actionCallback = delegate(InputAction.CallbackContext context)
            {
                OnTouchAction(index, context);
            };

            m_Touches.Add(newResponder);

            if (m_ActionsHooked)
                newResponder.HookActions();

            return id;
        }

        /// <summary>
        /// Adds Tracked Device UI responses based the Actions provided.
        /// </summary>
        /// <param name="position">A <see cref="Vector3"/> unity world space position that represents the position in the world of the device.</param>
        /// <param name="orientation">A <see cref="Quaternion"/> rotation value representing the orientation of the device.</param>
        /// <param name="select">A <see cref="bool"/> selection value that represents whether the user wants to select objects or not.</param>
        /// <returns>The Pointer Id that represents UI events from this Touch action set.</returns>
        public int AddTrackedDevice(InputActionProperty position, InputActionProperty orientation, InputActionProperty select)
        {
            var id = m_RollingPointerId++;

            var newResponder = new TrackedDeviceResponder(id, position, orientation, select);

            var index = m_TrackedDevices.Count;
            newResponder.actionCallback = delegate(InputAction.CallbackContext context)
            {
                OnTrackedDeviceAction(index, context);
            };

            m_TrackedDevices.Add(newResponder);

            if (m_ActionsHooked)
                newResponder.HookActions();

            return id;
        }

        /// <summary>
        /// This is a quick accessor for enabling all actions.  Currently, action ownership is ambiguous,
        /// and we need a way to enable/disable inspector-set actions.
        /// </summary>
        public void EnableAllActions()
        {
            var pointAction = m_PointAction.action;
            if (pointAction != null && !pointAction.enabled)
                pointAction.Enable();

            var leftClickAction = m_LeftClickAction.action;
            if (leftClickAction != null && !leftClickAction.enabled)
                leftClickAction.Enable();

            var rightClickAction = m_RightClickAction.action;
            if (rightClickAction != null && !rightClickAction.enabled)
                rightClickAction.Enable();

            var middleClickAction = m_MiddleClickAction.action;
            if (middleClickAction != null && !middleClickAction.enabled)
                middleClickAction.Enable();

            var moveAction = m_MoveAction.action;
            if (moveAction != null && !moveAction.enabled)
                moveAction.Enable();

            var submitAction = m_SubmitAction.action;
            if (submitAction != null && !submitAction.enabled)
                submitAction.Enable();

            var cancelAction = m_CancelAction.action;
            if (cancelAction != null && !cancelAction.enabled)
                cancelAction.Enable();

            for (int i = 0; i < m_Touches.Count; i++)
            {
                var touch = m_Touches[i];

                var positionAction = touch.position.action;
                if (positionAction != null && !positionAction.enabled)
                    positionAction.Enable();

                var phaseAction = touch.phase.action;
                if (phaseAction != null && !phaseAction.enabled)
                    phaseAction.Enable();
            }

            for (int i = 0; i < m_TrackedDevices.Count; i++)
            {
                var trackedDevice = m_TrackedDevices[i];

                var positionAction = trackedDevice.position.action;
                if (positionAction != null && !positionAction.enabled)
                    positionAction.Enable();

                var orientationAction = trackedDevice.orientation.action;
                if (orientationAction != null && !orientationAction.enabled)
                    orientationAction.Enable();

                var selectAction = trackedDevice.select.action;
                if (selectAction != null && !selectAction.enabled)
                    selectAction.Enable();
            }
        }

        /// <summary>
        /// This is a quick accessor for disabling all actions currently enabled.  Currently, action ownership is ambiguous,
        /// and we need a way to enable/disable inspector-set actions.
        /// </summary>
        public void DisableAllActions()
        {
            var pointAction = m_PointAction.action;
            if (pointAction != null && pointAction.enabled)
                pointAction.Disable();

            var leftClickAction = m_LeftClickAction.action;
            if (leftClickAction != null && leftClickAction.enabled)
                leftClickAction.Disable();

            var rightClickAction = m_RightClickAction.action;
            if (rightClickAction != null && rightClickAction.enabled)
                rightClickAction.Disable();

            var middleClickAction = m_MiddleClickAction.action;
            if (middleClickAction != null && middleClickAction.enabled)
                middleClickAction.Disable();

            var moveAction = m_MoveAction.action;
            if (moveAction != null && moveAction.enabled)
                moveAction.Disable();

            var submitAction = m_SubmitAction.action;
            if (submitAction != null && submitAction.enabled)
                submitAction.Disable();

            var cancelAction = m_CancelAction.action;
            if (cancelAction != null && cancelAction.enabled)
                cancelAction.Disable();

            for (int i = 0; i < m_Touches.Count; i++)
            {
                var touch = m_Touches[i];

                var positionAction = touch.position.action;
                if (positionAction != null && positionAction.enabled)
                    positionAction.Disable();

                var phaseAction = touch.phase.action;
                if (phaseAction != null && phaseAction.enabled)
                    phaseAction.Disable();
            }

            for (int i = 0; i < m_TrackedDevices.Count; i++)
            {
                var trackedDevice = m_TrackedDevices[i];

                var positionAction = trackedDevice.position.action;
                if (positionAction != null && positionAction.enabled)
                    positionAction.Disable();

                var orientationAction = trackedDevice.orientation.action;
                if (orientationAction != null && orientationAction.enabled)
                    orientationAction.Disable();

                var selectAction = trackedDevice.select.action;
                if (selectAction != null && selectAction.enabled)
                    selectAction.Disable();
            }
        }

        void OnAction(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == m_PointAction)
            {
                mouseState.position = context.ReadValue<Vector2>();
            }
            else if (action == m_ScrollWheelAction)
            {
                mouseState.scrollPosition = context.ReadValue<Vector2>();
            }
            else if (action == m_LeftClickAction)
            {
                var buttonState = mouseState.leftButton;
                buttonState.isDown = context.ReadValue<float>() != 0.0f;
                mouseState.leftButton = buttonState;
            }
            else if (action == m_RightClickAction)
            {
                var buttonState = mouseState.rightButton;
                buttonState.isDown = context.ReadValue<float>() != 0.0f;
                mouseState.rightButton = buttonState;
            }
            else if (action == m_MiddleClickAction)
            {
                var buttonState = mouseState.middleButton;
                buttonState.isDown = context.ReadValue<float>() != 0.0f;
                mouseState.middleButton = buttonState;
            }
            else if (action == m_MoveAction)
            {
                joystickState.move = context.ReadValue<Vector2>();
            }
            else if (action == m_SubmitAction)
            {
                joystickState.submitButtonDown = context.ReadValue<float>() != 0.0f;
            }
            else if (action == m_CancelAction)
            {
                joystickState.cancelButtonDown = context.ReadValue<float>() != 0.0f;
            }
        }

        void OnTouchAction(int touchIndex, InputAction.CallbackContext context)
        {
            if (touchIndex >= 0 && touchIndex < m_Touches.Count)
            {
                var responder = m_Touches[touchIndex];

                var action = context.action;
                if (action == responder.position)
                {
                    responder.state.position = context.ReadValue<Vector2>();
                }
                if (action == responder.phase)
                {
                    responder.state.selectPhase = context.ReadValue<PointerPhase>();
                }

                m_Touches[touchIndex] = responder;
            }
        }

        void OnTrackedDeviceAction(int deviceIndex, InputAction.CallbackContext context)
        {
            if (deviceIndex >= 0 && deviceIndex < m_TrackedDevices.Count)
            {
                var responder = m_TrackedDevices[deviceIndex];

                var action = context.action;
                if (action == responder.position)
                {
                    responder.state.position = context.ReadValue<Vector3>();
                }
                if (action == responder.orientation)
                {
                    responder.state.orientation = context.ReadValue<Quaternion>();
                }
                if (action == responder.select)
                {
                    responder.state.select = context.ReadValue<float>() != 0.0f;
                }

                m_TrackedDevices[deviceIndex] = responder;
            }
        }

        public override void Process()
        {
            DoProcess();
        }

        private void DoProcess()
        {
            // Reset devices of changes since we don't want to spool up changes once we gain focus.
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                joystickState.OnFrameFinished();
                mouseState.OnFrameFinished();

                for (int i = 0; i < m_Touches.Count; i++)
                    m_Touches[i].state.OnFrameFinished();

                for (int i = 0; i < m_TrackedDevices.Count; i++)
                    m_TrackedDevices[i].state.OnFrameFinished();
            }
            else
            {
                ProcessJoystick(ref joystickState);
                ProcessMouse(ref mouseState);

                for (int i = 0; i < m_Touches.Count; i++)
                {
                    var responder = m_Touches[i];
                    ProcessTouch(ref responder.state);
                    m_Touches[i] = responder;
                }

                for (int i = 0; i < m_TrackedDevices.Count; i++)
                {
                    var responder = m_TrackedDevices[i];
                    ProcessTrackedDevice(ref responder.state);
                    m_TrackedDevices[i] = responder;
                }
            }
        }

        private void HookActions()
        {
            if (m_ActionsHooked)
                return;

            m_ActionsHooked = true;

            var pointAction = m_PointAction.action;
            if (pointAction != null)
                pointAction.performed += OnAction;

            var moveAction = m_MoveAction.action;
            if (moveAction != null)
                moveAction.performed += OnAction;

            var leftClickAction = m_LeftClickAction.action;
            if (leftClickAction != null)
                leftClickAction.performed += OnAction;

            var rightClickAction = m_RightClickAction.action;
            if (rightClickAction != null)
                rightClickAction.performed += OnAction;

            var middleClickAction = m_MiddleClickAction.action;
            if (middleClickAction != null)
                middleClickAction.performed += OnAction;

            var submitAction = m_SubmitAction.action;
            if (submitAction != null)
                submitAction.performed += OnAction;

            var cancelAction = m_CancelAction.action;
            if (cancelAction != null)
                cancelAction.performed += OnAction;

            for (int i = 0; i < m_Touches.Count; i++)
            {
                var responder = m_Touches[i];
                responder.HookActions();
                m_Touches[i] = responder;
            }


            for (int i = 0; i < m_TrackedDevices.Count; i++)
            {
                var responder = m_TrackedDevices[i];
                responder.HookActions();
                m_TrackedDevices[i] = responder;
            }
        }

        private void UnhookActions()
        {
            if (!m_ActionsHooked)
                return;

            m_ActionsHooked = false;

            var pointAction = m_PointAction.action;
            if (pointAction != null)
                pointAction.performed -= OnAction;

            var moveAction = m_MoveAction.action;
            if (moveAction != null)
                moveAction.performed -= OnAction;

            var leftClickAction = m_LeftClickAction.action;
            if (leftClickAction != null)
                leftClickAction.performed -= OnAction;

            var rightClickAction = m_RightClickAction.action;
            if (rightClickAction != null)
                rightClickAction.performed -= OnAction;

            var middleClickAction = m_MiddleClickAction.action;
            if (middleClickAction != null)
                middleClickAction.performed -= OnAction;

            var submitAction = m_SubmitAction.action;
            if (submitAction != null)
                submitAction.performed -= OnAction;

            var cancelAction = m_CancelAction.action;
            if (cancelAction != null)
                cancelAction.performed -= OnAction;

            for (int i = 0; i < m_Touches.Count; i++)
            {
                var responder = m_Touches[i];
                responder.UnhookActions();
                m_Touches[i] = responder;
            }

            for (int i = 0; i < m_TrackedDevices.Count; i++)
            {
                var responder = m_TrackedDevices[i];
                responder.UnhookActions();
                m_TrackedDevices[i] = responder;
            }
        }

        [Tooltip("Enables UI events regardless of focus state.")]
        [SerializeField]
        private bool m_SendEventsWhenInBackground;

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D screen position
        /// </see> used as a cursor for pointing at UI elements.
        /// </summary>
        [Tooltip("Action that delivers a Vector2 of screen coordinates.")]
        [SerializeField]
        private InputActionProperty m_PointAction;

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        [Tooltip("Action that delivers a relative motion Vector2 for navigation.")]
        [SerializeField]
        private InputActionProperty m_MoveAction;

        [Tooltip("Button action that represents a 'Submit' navigation action.")]
        [SerializeField]
        private InputActionProperty m_SubmitAction;

        [Tooltip("Button action that represents a 'Cancel' navigation action.")]
        [SerializeField]
        private InputActionProperty m_CancelAction;

        [Tooltip("Button action that represents a left click.")]
        [SerializeField]
        private InputActionProperty m_LeftClickAction;

        [Tooltip("Button action that represents a middle click.")]
        [SerializeField]
        private InputActionProperty m_MiddleClickAction;

        [Tooltip("Button action that represents a right click.")]
        [SerializeField]
        private InputActionProperty m_RightClickAction;

        [Tooltip("Vector2 action that represents horizontal and vertical scrolling.")]
        [SerializeField]
        private InputActionProperty m_ScrollWheelAction;

        [SerializeField]
        private List<TouchResponder> m_Touches;

        [SerializeField]
        private List<TrackedDeviceResponder> m_TrackedDevices;

        [NonSerialized] private int m_RollingPointerId;
        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private bool m_ActionsEnabled;

        [NonSerialized] private MouseModel mouseState;
        [NonSerialized] private JoystickModel joystickState;
    }
}
