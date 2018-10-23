using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

////TODO: come up with an action response system that doesn't require hooking and unhooking all those delegates

//touch vs mouse will need refinement in both the action and the device stuff

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
        private static void SwapProperty(ref InputActionProperty oldProperty, InputActionProperty newProperty, bool actionsHooked, Action<InputAction.CallbackContext> actionCallback)
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

        struct TouchResponder
        {
            public TouchModel state;

            public InputActionProperty position
            {
                get { return m_Position; }
                set { SwapProperty(ref m_Position, value, m_ActionsHooked, OnAction); }
            }

            public InputActionProperty phase
            {
                get { return m_Phase; }
                set { SwapProperty(ref m_Phase, value, m_ActionsHooked, OnAction); }
            }

            private bool m_ActionsHooked;
            [SerializeField]
            private InputActionProperty m_Position;
            [SerializeField]
            private InputActionProperty m_Phase;

            void HookActions()
            {
                if (!m_ActionsHooked)
                {
                    m_ActionsHooked = true;

                    var positionAction = m_Position.action;
                    if (positionAction != null)
                        positionAction.performed += OnAction;

                    var phaseAction = m_Phase.action;
                    if (phaseAction != null)
                        phaseAction.performed += OnAction;
                }
            }

            void UnhookActions()
            {
                if (m_ActionsHooked)
                {
                    if (!m_ActionsHooked)
                    {
                        m_ActionsHooked = false;

                        var positionAction = m_Position.action;
                        if (positionAction != null)
                            positionAction.performed -= OnAction;

                        var phaseAction = m_Phase.action;
                        if (phaseAction != null)
                            phaseAction.performed -= OnAction;
                    }
                }
            }

            private void SwapProperty(ref InputActionProperty oldProperty, InputActionProperty newProperty)
            {
                if (oldProperty != null)
                {
                    if (m_ActionsHooked)
                        oldProperty.action.performed -= OnAction;
                }

                oldProperty = newProperty;

                if (oldProperty != null)
                {
                    if (m_ActionsHooked)
                        oldProperty.action.performed += OnAction;
                }
            }

            public void OnAction(InputAction.CallbackContext context)
            {
                var action = context.action;
                if (action == m_Position)
                    state.position = context.ReadValue<Vector2>();
                else if (action == m_Phase)
                    state.selectPhase = context.ReadValue<PointerPhase>();
            }
        }

        public bool ForceEventsWithoutFocus
        {
            get { return m_ForceInputWithoutFocus; }
            set { m_ForceInputWithoutFocus = value; }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D screen position
        /// </see> used as a cursor for pointing at UI elements.
        /// </summary>
        public InputActionProperty point
        {
            get { return m_PointAction; }
            set { SwapProperty(ref m_PointAction, value); }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        public InputActionProperty move
        {
            get { return m_MoveAction; }
            set { SwapProperty(ref m_MoveAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty scrollWheel
        {
            get { return m_ScrollWheelAction; }
            set { SwapProperty(ref m_ScrollWheelAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty leftClick
        {
            get { return m_LeftClickAction; }
            set { SwapProperty(ref m_LeftClickAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty middleClick
        {
            get { return m_MiddleClickAction; }
            set { SwapProperty(ref m_MiddleClickAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty rightClick
        {
            get { return m_RightClickAction; }
            set { SwapProperty(ref m_RightClickAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty submit
        {
            get { return m_SubmitAction; }
            set { SwapProperty(ref m_SubmitAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty cancel
        {
            get { return m_CancelAction; }
            set { SwapProperty(ref m_CancelAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        //XR Stuff
        public InputActionProperty trackedPosition
        {
            get { return m_TrackedPositionAction; }
            set { SwapProperty(ref m_TrackedPositionAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty trackedOrientation
        {
            get { return m_TrackedOrientationAction; }
            set { SwapProperty(ref m_TrackedOrientationAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty trackedSelect
        {
            get { return m_TrackedSelectAction; }
            set { SwapProperty(ref m_TrackedSelectAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        //Touch Stuff
        public InputActionProperty touchPosition
        {
            get { return m_TouchPositionAction; }
            set { SwapProperty(ref m_TouchPositionAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        public InputActionProperty touchPhase
        {
            get { return m_TouchPhaseAction; }
            set { SwapProperty(ref m_TouchPhaseAction, value, m_ActionsHooked, m_ActionCallback); }
        }

        protected override void Awake()
        {
            base.Awake();

            int rollingPointerIndex = 0;
            mouseState = new MouseModel(rollingPointerIndex++);
            joystickState.Reset();
            trackedDeviceState = new TrackedDeviceModel(rollingPointerIndex++);
            touchState = new TouchModel(rollingPointerIndex++);
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
            if (m_ForceInputWithoutFocus)
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

        public void OnAction(InputAction.CallbackContext context)
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
            else if (action == m_TrackedPositionAction)
            {
                trackedDeviceState.position = context.ReadValue<Vector3>();
            }
            else if (action == m_TrackedOrientationAction)
            {
                trackedDeviceState.orientation = context.ReadValue<Quaternion>();
            }
            else if (action == m_TrackedSelectAction)
            {
                trackedDeviceState.select = context.ReadValue<float>() != 0.0f;
            }
            else if (action == m_TouchPositionAction)
            {
                touchState.position = context.ReadValue<Vector2>();
            }
            else if (action == m_TouchPhaseAction)
            {
                touchState.selectPhase = context.ReadValue<PointerPhase>();
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
                trackedDeviceState.OnFrameFinished();
                touchState.OnFrameFinished();
            }
            else
            {
                ProcessJoystick(ref joystickState);
                ProcessMouse(ref mouseState);
                ProcessTrackedDevice(ref trackedDeviceState);
                ProcessTouch(ref touchState);
            }
        }

        private void HookActions()
        {
            if (m_ActionsHooked)
                return;

            if (m_ActionCallback == null)
                m_ActionCallback = OnAction;

            m_ActionsHooked = true;

            var pointAction = m_PointAction.action;
            if (pointAction != null)
                pointAction.performed += m_ActionCallback;

            var moveAction = m_MoveAction.action;
            if (moveAction != null)
                moveAction.performed += m_ActionCallback;

            var leftClickAction = m_LeftClickAction.action;
            if (leftClickAction != null)
                leftClickAction.performed += m_ActionCallback;

            var rightClickAction = m_RightClickAction.action;
            if (rightClickAction != null)
                rightClickAction.performed += m_ActionCallback;

            var middleClickAction = m_MiddleClickAction.action;
            if (middleClickAction != null)
                middleClickAction.performed += m_ActionCallback;

            var submitAction = m_SubmitAction.action;
            if (submitAction != null)
                submitAction.performed += m_ActionCallback;

            var cancelAction = m_CancelAction.action;
            if (cancelAction != null)
                cancelAction.performed += m_ActionCallback;

            var trackedPositionAction = m_TrackedPositionAction.action;
            if (trackedPositionAction != null)
                trackedPositionAction.performed += m_ActionCallback;

            var trackedOrientationAction = m_TrackedOrientationAction.action;
            if (trackedOrientationAction != null)
                trackedOrientationAction.performed += m_ActionCallback;

            var trackedSelectAction = m_TrackedSelectAction.action;
            if (trackedSelectAction != null)
                trackedSelectAction.performed += m_ActionCallback;

            var touchPositionAction = m_TouchPositionAction.action;
            if (touchPositionAction != null)
                touchPositionAction.performed += m_ActionCallback;

            var touchPhaseAction = m_TouchPhaseAction.action;
            if (touchPhaseAction != null)
                touchPhaseAction.performed += m_ActionCallback;
        }

        private void UnhookActions()
        {
            if (!m_ActionsHooked)
                return;

            m_ActionsHooked = false;

            var pointAction = m_PointAction.action;
            if (pointAction != null)
                pointAction.performed -= m_ActionCallback;

            var moveAction = m_MoveAction.action;
            if (moveAction != null)
                moveAction.performed -= m_ActionCallback;

            var leftClickAction = m_LeftClickAction.action;
            if (leftClickAction != null)
                leftClickAction.performed -= m_ActionCallback;

            var rightClickAction = m_RightClickAction.action;
            if (rightClickAction != null)
                rightClickAction.performed -= m_ActionCallback;

            var middleClickAction = m_MiddleClickAction.action;
            if (middleClickAction != null)
                middleClickAction.performed -= m_ActionCallback;

            var submitAction = m_SubmitAction.action;
            if (submitAction != null)
                submitAction.performed -= m_ActionCallback;

            var cancelAction = m_CancelAction.action;
            if (cancelAction != null)
                cancelAction.performed -= m_ActionCallback;

            var trackedPositionAction = m_TrackedPositionAction.action;
            if (trackedPositionAction != null)
                trackedPositionAction.performed -= m_ActionCallback;

            var trackedOrientationAction = m_TrackedOrientationAction.action;
            if (trackedOrientationAction != null)
                trackedOrientationAction.performed -= m_ActionCallback;

            var trackedSelectAction = m_TrackedSelectAction.action;
            if (trackedSelectAction != null)
                trackedSelectAction.performed -= m_ActionCallback;

            var touchPositionAction = m_TouchPositionAction.action;
            if (touchPositionAction != null)
                touchPositionAction.performed -= m_ActionCallback;

            var touchPhaseAction = m_TouchPhaseAction.action;
            if (touchPhaseAction != null)
                touchPhaseAction.performed -= m_ActionCallback;
        }

        private void SwapProperty(ref InputActionProperty oldProperty, InputActionProperty newProperty)
        {
            if (oldProperty != null)
            {
                if (m_ActionsHooked)
                    oldProperty.action.performed -= m_ActionCallback;
            }

            oldProperty = newProperty;

            if (oldProperty != null)
            {
                if (m_ActionsHooked)
                    oldProperty.action.performed += m_ActionCallback;
            }
        }

        [Tooltip("Enables UI events regardless of focus state.")]
        [SerializeField]
        private bool m_ForceInputWithoutFocus;

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
        private InputActionProperty m_TrackedPositionAction;

        [SerializeField]
        private InputActionProperty m_TrackedOrientationAction;

        [SerializeField]
        private InputActionProperty m_TrackedSelectAction;

        [SerializeField]
        private InputActionProperty m_TouchPositionAction;

        [SerializeField]
        private InputActionProperty m_TouchPhaseAction;

        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private bool m_ActionsEnabled;
        [NonSerialized] private Action<InputAction.CallbackContext> m_ActionCallback;

        [NonSerialized] private MouseModel mouseState;
        [NonSerialized] private JoystickModel joystickState;
        [NonSerialized] private TrackedDeviceModel trackedDeviceState;
        [NonSerialized] private TouchModel touchState;
    }
}
