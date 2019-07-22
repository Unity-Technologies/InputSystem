using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

////REVIEW: should each of the actions be *lists* of actions?

////TODO: add ability to query which device was last used with any of the actions

////TODO: come up with an action response system that doesn't require hooking and unhooking all those delegates

////TODO: touch vs mouse will need refinement in both the action and the device stuff

namespace UnityEngine.InputSystem.UI
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


        public InputActionReference trackedDeviceOrientation
        {
            get => m_TrackedDeviceOrientationAction;
            set => SwapAction(ref m_TrackedDeviceOrientationAction, value, m_ActionsHooked, OnAction);
        }

        public InputActionReference trackedDevicePosition
        {
            get => m_TrackedDevicePositionAction;
            set => SwapAction(ref m_TrackedDevicePositionAction, value, m_ActionsHooked, OnAction);
        }

        public InputActionReference trackedDeviceSelect
        {
            get => m_TrackedDeviceSelectAction;
            set => SwapAction(ref m_TrackedDeviceSelectAction, value, m_ActionsHooked, OnAction);
        }


        protected override void Awake()
        {
            base.Awake();

            m_RollingPointerId = 0;
            mouseState = new MouseModel(m_RollingPointerId++);
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
            EnableAllActions();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            DisableAllActions();
            UnhookActions();
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
                (m_ScrollWheelAction?.action?.enabled ?? true) &&
                (m_TrackedDeviceOrientationAction?.action?.enabled ?? true) &&
                (m_TrackedDevicePositionAction?.action?.enabled ?? true) &&
                (m_TrackedDeviceSelectAction?.action?.enabled ?? true);
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
                m_TrackedDeviceOrientationAction?.action?.Enable();
                m_TrackedDevicePositionAction?.action?.Enable();
                m_TrackedDeviceSelectAction?.action?.Enable();
                m_OwnsEnabledState = true;
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
                m_TrackedDeviceOrientationAction?.action?.Disable();
                m_TrackedDevicePositionAction?.action?.Disable();
                m_TrackedDeviceSelectAction?.action?.Disable();
            }
        }

        int GetTrackedDeviceIndexForCallbackContext(InputAction.CallbackContext context)
        {
            Debug.Assert(context.action.type == InputActionType.PassThrough, $"XR actions should be pass-through actions, so the UI can properly distinguish multiple tracked devices. Please set the action type of '{context.action.name}' to 'Pass-Through'.");
            for (var i = 0; i < trackedDeviceStates.Count; i++)
            {
                if (trackedDeviceStates[i].device == context.control.device)
                    return i;
            }
            trackedDeviceStates.Add(new TrackedDeviceModel(m_RollingPointerId++, context.control.device));
            return trackedDeviceStates.Count - 1;
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
                mouseState.scrollDelta = context.ReadValue<Vector2>() * (1.0f / kPixelPerLine);
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
            else if (action == m_TrackedDeviceOrientationAction?.action)
            {
                var index = GetTrackedDeviceIndexForCallbackContext(context);
                var state = trackedDeviceStates[index];
                state.orientation = context.ReadValue<Quaternion>();
                trackedDeviceStates[index] = state;
            }
            else if (action == m_TrackedDevicePositionAction?.action)
            {
                var index = GetTrackedDeviceIndexForCallbackContext(context);
                var state = trackedDeviceStates[index];
                state.position = context.ReadValue<Vector3>();
                trackedDeviceStates[index] = state;
            }
            else if (action == m_TrackedDeviceSelectAction?.action)
            {
                var index = GetTrackedDeviceIndexForCallbackContext(context);
                var state = trackedDeviceStates[index];
                state.select = context.ReadValue<float>() > 0;
                trackedDeviceStates[index] = state;
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
                foreach (var trackedDeviceState in trackedDeviceStates)
                    trackedDeviceState.OnFrameFinished();
            }
            else
            {
                ProcessJoystick(ref joystickState);
                ProcessMouse(ref mouseState);

                for (var i = 0; i < trackedDeviceStates.Count; i++)
                {
                    var state = trackedDeviceStates[i];
                    ProcessTrackedDevice(ref state);
                    trackedDeviceStates[i] = state;
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

            var trackedDeviceOrientationAction = m_TrackedDeviceOrientationAction?.action;
            if (trackedDeviceOrientationAction != null)
            {
                trackedDeviceOrientationAction.performed += m_OnActionDelegate;
                trackedDeviceOrientationAction.canceled += m_OnActionDelegate;
            }

            var trackedDevicePositionAction = m_TrackedDevicePositionAction?.action;
            if (trackedDeviceOrientationAction != null)
            {
                trackedDevicePositionAction.performed += m_OnActionDelegate;
                trackedDevicePositionAction.canceled += m_OnActionDelegate;
            }

            var trackedDeviceSelectAction = m_TrackedDeviceSelectAction?.action;
            if (trackedDeviceSelectAction != null)
            {
                trackedDeviceSelectAction.performed += m_OnActionDelegate;
                trackedDeviceSelectAction.canceled += m_OnActionDelegate;
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
                scrollAction.performed -= m_OnActionDelegate;
                scrollAction.canceled -= m_OnActionDelegate;
            }

            var trackedDeviceOrientationAction = m_TrackedDeviceOrientationAction?.action;
            if (trackedDeviceOrientationAction != null)
            {
                trackedDeviceOrientationAction.performed -= m_OnActionDelegate;
                trackedDeviceOrientationAction.canceled -= m_OnActionDelegate;
            }

            var trackedDevicePositionAction = m_TrackedDevicePositionAction?.action;
            if (trackedDeviceOrientationAction != null)
            {
                trackedDevicePositionAction.performed -= m_OnActionDelegate;
                trackedDevicePositionAction.canceled -= m_OnActionDelegate;
            }

            var trackedDeviceSelectAction = m_TrackedDeviceSelectAction?.action;
            if (trackedDeviceSelectAction != null)
            {
                trackedDeviceSelectAction.performed -= m_OnActionDelegate;
                trackedDeviceSelectAction.canceled -= m_OnActionDelegate;
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

        [SerializeField, HideInInspector] private InputActionReference m_TrackedDevicePositionAction;
        [SerializeField, HideInInspector] private InputActionReference m_TrackedDeviceOrientationAction;
        [SerializeField, HideInInspector] private InputActionReference m_TrackedDeviceSelectAction;

        [NonSerialized] private int m_RollingPointerId;
        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private Action<InputAction.CallbackContext> m_OnActionDelegate;

        [NonSerialized] private MouseModel mouseState;
        [NonSerialized] private JoystickModel joystickState;
        [NonSerialized] private List<TrackedDeviceModel> trackedDeviceStates = new List<TrackedDeviceModel>();
    }
}
