using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

////REVIEW: should each of the actions be *lists* of actions?

////TODO: add ability to query which device was last used with any of the actions

////TODO: come up with an action response system that doesn't require hooking and unhooking all those delegates

////TODO: touch vs mouse will need refinement in both the action and the device stuff

namespace UnityEngine.InputSystem.Plugins.UI
{
    /// <summary>
    /// Input module that takes its input from <see cref="InputAction">input actions</see>.
    /// </summary>
    /// <remarks>
    /// This UI input module has the advantage over other such modules that it doesn't have to know
    /// what devices and types of devices input is coming from. Instead, the actions hide the actual
    /// sources of input from the module.
    /// </remarks>
    public class InputSystemUIInputModule : UIInputModule
    {
        private static void SwapAction(ref InputActionReference property, InputActionReference newValue, bool actionsHooked, Action<InputAction.CallbackContext> actionCallback)
        {
            if (property != null && actionsHooked)
            {
                property.action.performed -= actionCallback;
                property.action.canceled -= actionCallback;
            }

            property = newValue;

            if (newValue != null && actionsHooked)
            {
                property.action.performed += actionCallback;
                property.action.canceled += actionCallback;
            }
        }

        // Todo: Figure out what we will do with touch and tracked devices. Should those actions also become InputActionReference?
        private static void SwapAction(ref InputActionProperty property, InputActionProperty newValue, bool actionsHooked, Action<InputAction.CallbackContext> actionCallback)
        {
            if (property != null && actionsHooked)
            {
                property.action.performed -= actionCallback;
                property.action.canceled -= actionCallback;
            }

            property = newValue;

            if (newValue != null && actionsHooked)
            {
                property.action.performed += actionCallback;
                property.action.canceled += actionCallback;
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
                get => m_Position;
                set => SwapAction(ref m_Position, value, m_ActionsHooked, actionCallback);
            }

            public InputActionProperty phase
            {
                get => m_Phase;
                set => SwapAction(ref m_Phase, value, m_ActionsHooked, actionCallback);
            }

            public bool actionsHooked => m_ActionsHooked;

            public void HookActions()
            {
                if (m_ActionsHooked)
                    return;

                m_ActionsHooked = true;

                var positionAction = m_Position.action;
                if (positionAction != null)
                {
                    positionAction.performed += actionCallback;
                    positionAction.canceled += actionCallback;
                }

                var phaseAction = m_Phase.action;
                if (phaseAction != null)
                {
                    phaseAction.performed += actionCallback;
                    phaseAction.canceled += actionCallback;
                }
            }

            public void UnhookActions()
            {
                if (!m_ActionsHooked)
                    return;

                m_ActionsHooked = false;

                var positionAction = m_Position.action;
                if (positionAction != null)
                {
                    positionAction.performed -= actionCallback;
                    positionAction.canceled -= actionCallback;
                }

                var phaseAction = m_Phase.action;
                if (phaseAction != null)
                {
                    phaseAction.performed -= actionCallback;
                    phaseAction.canceled -= actionCallback;
                }
            }

            private bool m_ActionsHooked;

            [SerializeField] private InputActionProperty m_Position;
            [SerializeField] private InputActionProperty m_Phase;
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
                get => m_Position;
                set => SwapAction(ref m_Position, value, m_ActionsHooked, actionCallback);
            }

            public InputActionProperty orientation
            {
                get => m_Orientation;
                set => SwapAction(ref m_Orientation, value, m_ActionsHooked, actionCallback);
            }

            public InputActionProperty select
            {
                get => m_Select;
                set => SwapAction(ref m_Select, value, m_ActionsHooked, actionCallback);
            }

            public bool actionsHooked => m_ActionsHooked;

            public void HookActions()
            {
                if (m_ActionsHooked)
                    return;

                m_ActionsHooked = true;

                var positionAction = m_Position.action;
                if (positionAction != null)
                {
                    positionAction.performed += actionCallback;
                    positionAction.canceled += actionCallback;
                }

                var orientationAction = m_Orientation.action;
                if (orientationAction != null)
                {
                    orientationAction.performed += actionCallback;
                    orientationAction.canceled += actionCallback;
                }

                var selectAction = m_Select.action;
                if (selectAction != null)
                {
                    selectAction.performed += actionCallback;
                    selectAction.canceled += actionCallback;
                }
            }

            public void UnhookActions()
            {
                if (!m_ActionsHooked)
                    return;

                m_ActionsHooked = false;

                var positionAction = m_Position.action;
                if (positionAction != null)
                {
                    positionAction.performed -= actionCallback;
                    positionAction.canceled -= actionCallback;
                }

                var orientationAction = m_Orientation.action;
                if (orientationAction != null)
                {
                    orientationAction.performed -= actionCallback;
                    orientationAction.canceled -= actionCallback;
                }

                var selectAction = m_Orientation.action;
                if (selectAction != null)
                {
                    selectAction.performed -= actionCallback;
                    selectAction.canceled -= actionCallback;
                }
            }

            private bool m_ActionsHooked;

            [SerializeField] private InputActionProperty m_Position;
            [SerializeField] private InputActionProperty m_Orientation;
            [SerializeField] private InputActionProperty m_Select;
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D screen position.
        /// </see> used as a cursor for pointing at UI elements.
        /// </summary>
        public InputActionReference point
        {
            get => m_PointAction;
            set => SwapAction(ref m_PointAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector.
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        public InputActionReference move
        {
            get => m_MoveAction;
            set => SwapAction(ref m_MoveAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2"> scroll wheel value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference scrollWheel
        {
            get => m_ScrollWheelAction;
            set => SwapAction(ref m_ScrollWheelAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference leftClick
        {
            get => m_LeftClickAction;
            set => SwapAction(ref m_LeftClickAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference middleClick
        {
            get => m_MiddleClickAction;
            set => SwapAction(ref m_MiddleClickAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference rightClick
        {
            get => m_RightClickAction;
            set => SwapAction(ref m_RightClickAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="BaseEventData"/> events.
        /// </summary>
        public InputActionReference submit
        {
            get => m_SubmitAction;
            set => SwapAction(ref m_SubmitAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="bool"> button value.
        /// </see> used for sending <see cref="BaseEventData"/> events.
        /// </summary>
        public InputActionReference cancel
        {
            get => m_CancelAction;
            set => SwapAction(ref m_CancelAction, value, m_ActionsHooked, OnAction);
        }

        protected override void Awake()
        {
            base.Awake();

            m_RollingPointerId = 0;
            mouseState = new MouseModel(m_RollingPointerId++);
            joystickState.Reset();

            if (m_Touches == null)
                m_Touches = new List<TouchResponder>();

            for (var i = 0; i < m_Touches.Count; i++)
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

            for (var i = 0; i < m_TrackedDevices.Count; i++)
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
            EnableAllActions();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DisableAllActions();
            UnhookActions();
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

        bool IsAnyActionEnabled()
        {
            return (m_PointAction?.action?.enabled ?? true) &&
                (m_LeftClickAction?.action?.enabled ?? true) &&
                (m_RightClickAction?.action?.enabled ?? true) &&
                (m_MiddleClickAction?.action?.enabled ?? true) &&
                (m_MoveAction?.action?.enabled ?? true) &&
                (m_SubmitAction?.action?.enabled ?? true) &&
                (m_CancelAction?.action?.enabled ?? true) &&
                (m_ScrollWheelAction?.action?.enabled ?? true);
        }

        bool m_OwnsEnabledState;
        /// <summary>
        /// This is a quick accessor for enabling all actions.  Currently, action ownership is ambiguous,
        /// and we need a way to enable/disable inspector-set actions.
        /// </summary>
        public void EnableAllActions()
        {
            if (!IsAnyActionEnabled())
            {
                m_PointAction?.action?.Enable();
                m_LeftClickAction?.action?.Enable();
                m_RightClickAction?.action?.Enable();
                m_MiddleClickAction?.action?.Enable();
                m_MoveAction?.action?.Enable();
                m_SubmitAction?.action?.Enable();
                m_CancelAction?.action?.Enable();
                m_ScrollWheelAction?.action?.Enable();
                m_OwnsEnabledState = true;
            }

            for (var i = 0; i < m_Touches.Count; i++)
            {
                var touch = m_Touches[i];

                var positionAction = touch.position.action;
                positionAction?.Enable();

                var phaseAction = touch.phase.action;
                if (phaseAction != null && !phaseAction.enabled)
                    phaseAction.Enable();
            }

            for (var i = 0; i < m_TrackedDevices.Count; i++)
            {
                var trackedDevice = m_TrackedDevices[i];

                var positionAction = trackedDevice.position.action;
                positionAction?.Enable();

                var orientationAction = trackedDevice.orientation.action;
                orientationAction?.Enable();

                var selectAction = trackedDevice.select.action;
                selectAction?.Enable();
            }
        }

        /// <summary>
        /// This is a quick accessor for disabling all actions currently enabled.  Currently, action ownership is ambiguous,
        /// and we need a way to enable/disable inspector-set actions.
        /// </summary>
        public void DisableAllActions()
        {
            if (m_OwnsEnabledState)
            {
                m_OwnsEnabledState = false;
                m_PointAction?.action?.Disable();
                m_LeftClickAction?.action?.Disable();
                m_RightClickAction?.action?.Disable();
                m_MiddleClickAction?.action?.Disable();
                m_MoveAction?.action?.Disable();
                m_SubmitAction?.action?.Disable();
                m_CancelAction?.action?.Disable();
                m_ScrollWheelAction?.action?.Disable();
            }

            for (var i = 0; i < m_Touches.Count; i++)
            {
                var touch = m_Touches[i];

                var positionAction = touch.position.action;
                positionAction?.Disable();

                var phaseAction = touch.phase.action;
                phaseAction?.Disable();
            }

            for (var i = 0; i < m_TrackedDevices.Count; i++)
            {
                var trackedDevice = m_TrackedDevices[i];

                var positionAction = trackedDevice.position.action;
                positionAction?.Disable();

                var orientationAction = trackedDevice.orientation.action;
                orientationAction?.Disable();

                var selectAction = trackedDevice.select.action;
                if (selectAction != null)
                    selectAction.Disable();
            }
        }

        void OnAction(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == m_PointAction?.action)
            {
                mouseState.position = context.ReadValue<Vector2>();
            }
            else if (action == m_ScrollWheelAction?.action)
            {
                // The old input system reported scroll deltas in lines, we report pixels.
                // Need to scale as the UI system expects lines.
                const float kPixelPerLine = 20;
                mouseState.scrollPosition = context.ReadValue<Vector2>() * (1.0f / kPixelPerLine);
            }
            else if (action == m_LeftClickAction?.action)
            {
                var buttonState = mouseState.leftButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                mouseState.leftButton = buttonState;
            }
            else if (action == m_RightClickAction?.action)
            {
                var buttonState = mouseState.rightButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                mouseState.rightButton = buttonState;
            }
            else if (action == m_MiddleClickAction?.action)
            {
                var buttonState = mouseState.middleButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                mouseState.middleButton = buttonState;
            }
            else if (action == m_MoveAction?.action)
            {
                joystickState.move = context.ReadValue<Vector2>();
            }
            else if (action == m_SubmitAction?.action)
            {
                joystickState.submitButtonDown = context.ReadValue<float>() > 0;
            }
            else if (action == m_CancelAction?.action)
            {
                joystickState.cancelButtonDown = context.ReadValue<float>() > 0;
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
                    responder.state.select = context.ReadValue<float>() > 0;
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
            if (!eventSystem.isFocused)
            {
                joystickState.OnFrameFinished();
                mouseState.OnFrameFinished();

                for (var i = 0; i < m_Touches.Count; i++)
                    m_Touches[i].state.OnFrameFinished();

                for (var i = 0; i < m_TrackedDevices.Count; i++)
                    m_TrackedDevices[i].state.OnFrameFinished();
            }
            else
            {
                ProcessJoystick(ref joystickState);
                ProcessMouse(ref mouseState);

                for (var i = 0; i < m_Touches.Count; i++)
                {
                    var responder = m_Touches[i];
                    ProcessTouch(ref responder.state);
                    m_Touches[i] = responder;
                }

                for (var i = 0; i < m_TrackedDevices.Count; i++)
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
            if (m_OnActionDelegate == null)
                m_OnActionDelegate = OnAction;

            var pointAction = m_PointAction?.action;
            if (pointAction != null)
            {
                pointAction.performed += m_OnActionDelegate;
                pointAction.canceled += m_OnActionDelegate;
            }

            var moveAction = m_MoveAction?.action;
            if (moveAction != null)
            {
                moveAction.performed += m_OnActionDelegate;
                moveAction.canceled += m_OnActionDelegate;
            }

            var leftClickAction = m_LeftClickAction?.action;
            if (leftClickAction != null)
            {
                leftClickAction.performed += m_OnActionDelegate;
                leftClickAction.canceled += m_OnActionDelegate;
            }

            var rightClickAction = m_RightClickAction?.action;
            if (rightClickAction != null)
            {
                rightClickAction.performed += m_OnActionDelegate;
                rightClickAction.canceled += m_OnActionDelegate;
            }

            var middleClickAction = m_MiddleClickAction?.action;
            if (middleClickAction != null)
            {
                middleClickAction.performed += m_OnActionDelegate;
                middleClickAction.canceled += m_OnActionDelegate;
            }

            var submitAction = m_SubmitAction?.action;
            if (submitAction != null)
            {
                submitAction.performed += m_OnActionDelegate;
                submitAction.canceled += m_OnActionDelegate;
            }

            var cancelAction = m_CancelAction?.action;
            if (cancelAction != null)
            {
                cancelAction.performed += m_OnActionDelegate;
                cancelAction.canceled += m_OnActionDelegate;
            }

            var scrollAction = m_ScrollWheelAction?.action;
            if (scrollAction != null)
            {
                scrollAction.performed += m_OnActionDelegate;
                scrollAction.canceled += m_OnActionDelegate;
            }

            for (var i = 0; i < m_Touches.Count; i++)
            {
                var responder = m_Touches[i];
                responder.HookActions();
                m_Touches[i] = responder;
            }

            for (var i = 0; i < m_TrackedDevices.Count; i++)
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

            var pointAction = m_PointAction?.action;
            if (pointAction != null)
            {
                pointAction.performed -= m_OnActionDelegate;
                pointAction.canceled -= m_OnActionDelegate;
            }

            var moveAction = m_MoveAction?.action;
            if (moveAction != null)
            {
                moveAction.performed -= m_OnActionDelegate;
                moveAction.canceled -= m_OnActionDelegate;
            }

            var leftClickAction = m_LeftClickAction?.action;
            if (leftClickAction != null)
            {
                leftClickAction.performed -= m_OnActionDelegate;
                leftClickAction.canceled -= m_OnActionDelegate;
            }

            var rightClickAction = m_RightClickAction?.action;
            if (rightClickAction != null)
            {
                rightClickAction.performed -= m_OnActionDelegate;
                rightClickAction.canceled -= m_OnActionDelegate;
            }

            var middleClickAction = m_MiddleClickAction?.action;
            if (middleClickAction != null)
            {
                middleClickAction.performed -= m_OnActionDelegate;
                middleClickAction.canceled -= m_OnActionDelegate;
            }

            var submitAction = m_SubmitAction?.action;
            if (submitAction != null)
            {
                submitAction.performed -= m_OnActionDelegate;
                submitAction.canceled -= m_OnActionDelegate;
            }

            var cancelAction = m_CancelAction?.action;
            if (cancelAction != null)
            {
                cancelAction.performed -= m_OnActionDelegate;
                cancelAction.canceled -= m_OnActionDelegate;
            }

            var scrollAction = m_ScrollWheelAction?.action;
            if (scrollAction != null)
            {
                scrollAction.performed += m_OnActionDelegate;
                scrollAction.canceled += m_OnActionDelegate;
            }

            for (var i = 0; i < m_Touches.Count; i++)
            {
                var responder = m_Touches[i];
                responder.UnhookActions();
                m_Touches[i] = responder;
            }

            for (var i = 0; i < m_TrackedDevices.Count; i++)
            {
                var responder = m_TrackedDevices[i];
                responder.UnhookActions();
                m_TrackedDevices[i] = responder;
            }
        }

        private InputActionReference UpdateReferenceForNewAsset(InputActionReference actionReference)
        {
            if (actionReference?.action == null)
                return null;

            return InputActionReference.Create(m_ActionsAsset.FindAction(actionReference.action.name));
        }

        [SerializeField, HideInInspector] private InputActionAsset m_ActionsAsset;

        public InputActionAsset actionsAsset
        {
            get => m_ActionsAsset;
            set
            {
                if (value != m_ActionsAsset)
                {
                    var wasEnabled = IsAnyActionEnabled();
                    DisableAllActions();
                    m_ActionsAsset = value;

                    point = UpdateReferenceForNewAsset(point);
                    move = UpdateReferenceForNewAsset(move);
                    leftClick = UpdateReferenceForNewAsset(leftClick);
                    rightClick = UpdateReferenceForNewAsset(rightClick);
                    middleClick = UpdateReferenceForNewAsset(middleClick);
                    scrollWheel = UpdateReferenceForNewAsset(scrollWheel);
                    submit = UpdateReferenceForNewAsset(submit);
                    cancel = UpdateReferenceForNewAsset(cancel);
                    if (wasEnabled)
                        EnableAllActions();
                }
            }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D screen position
        /// </see> used as a cursor for pointing at UI elements.
        /// </summary>

        [SerializeField, HideInInspector] private InputActionReference m_PointAction;

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        [SerializeField, HideInInspector] private InputActionReference m_MoveAction;
        [SerializeField, HideInInspector] private InputActionReference m_SubmitAction;
        [SerializeField, HideInInspector] private InputActionReference m_CancelAction;
        [SerializeField, HideInInspector] private InputActionReference m_LeftClickAction;
        [SerializeField, HideInInspector] private InputActionReference m_MiddleClickAction;
        [SerializeField, HideInInspector] private InputActionReference m_RightClickAction;
        [SerializeField, HideInInspector] private InputActionReference m_ScrollWheelAction;

        // Hide these while we still have to figure out what to do with these.
        [SerializeField, HideInInspector] private List<TouchResponder> m_Touches = new List<TouchResponder>();
        [SerializeField, HideInInspector] private List<TrackedDeviceResponder> m_TrackedDevices = new List<TrackedDeviceResponder>();

        [NonSerialized] private int m_RollingPointerId;
        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private Action<InputAction.CallbackContext> m_OnActionDelegate;

        [NonSerialized] private MouseModel mouseState;
        [NonSerialized] private JoystickModel joystickState;
    }
}
