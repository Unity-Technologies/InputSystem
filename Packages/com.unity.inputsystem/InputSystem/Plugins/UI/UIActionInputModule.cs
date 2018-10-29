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
            set { SwapProperty(ref m_MoveAction, value); }
        }

        public InputActionProperty scrollWheel
        {
            get { return m_ScrollWheelAction; }
            set { SwapProperty(ref m_ScrollWheelAction, value); }
        }

        public InputActionProperty leftClick
        {
            get { return m_LeftClickAction; }
            set { SwapProperty(ref m_LeftClickAction, value); }
        }

        public InputActionProperty middleClick
        {
            get { return m_MiddleClickAction; }
            set { SwapProperty(ref m_MiddleClickAction, value); }
        }

        public InputActionProperty rightClick
        {
            get { return m_RightClickAction; }
            set { SwapProperty(ref m_RightClickAction, value); }
        }

        public InputActionProperty submit
        {
            get { return m_SubmitAction; }
            set { SwapProperty(ref m_SubmitAction, value); }
        }

        public InputActionProperty cancel
        {
            get { return m_CancelAction; }
            set { SwapProperty(ref m_CancelAction, value); }
        }

        protected override void Awake()
        {
            base.Awake();

            /// TODO TB: We don't have proper mouse pointer Ids atm, use 0 for single mouse state.
            mouseState = new MouseModel(eventSystem, 0);
            joystickState.Reset();
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
            }

            ProcessJoystick(ref joystickState);
            ProcessMouse(ref mouseState);
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

        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private bool m_ActionsEnabled;
        [NonSerialized] private Action<InputAction.CallbackContext> m_ActionCallback;

        [NonSerialized] private MouseModel mouseState;
        [NonSerialized] private JoystickModel joystickState;
    }
}
