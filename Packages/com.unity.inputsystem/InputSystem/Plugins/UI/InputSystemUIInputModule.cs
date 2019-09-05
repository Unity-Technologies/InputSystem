using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;

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
    public class InputSystemUIInputModule : BaseInputModule
    {
        public override void ActivateModule()
        {
            base.ActivateModule();

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        private RaycastResult PerformRaycast(PointerEventData eventData)
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            var result = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
            return result;
        }

        /// <summary>
        /// Takes an existing MouseModel and dispatches all relevant changes through the event system.
        /// It also updates the internal data of the MouseModel.
        /// </summary>
        /// <param name="mouseState">The mouse state you want to forward into the UI Event System</param>
        internal void ProcessMouse(ref MouseModel mouseState)
        {
            if (!mouseState.changedThisFrame)
                return;

            var eventData = GetOrCreateCachedPointerEvent();
            eventData.Reset();

            mouseState.CopyTo(eventData);

            eventData.pointerCurrentRaycast = PerformRaycast(eventData);

            /// Left Mouse Button
            // The left mouse button is 'dominant' and we want to also process hover and scroll events as if the occurred during the left click.
            var buttonState = mouseState.leftButton;
            buttonState.CopyTo(eventData);
            eventData.button = PointerEventData.InputButton.Left;

            ProcessMouseButton(buttonState.lastFrameDelta, eventData, buttonState.hasNativeClickCount);

            ProcessMouseMovement(eventData);
            ProcessMouseScroll(eventData);

            mouseState.CopyFrom(eventData);

            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.leftButton = buttonState;

            /// Right Mouse Button
            buttonState = mouseState.rightButton;
            buttonState.CopyTo(eventData);
            eventData.button = PointerEventData.InputButton.Right;

            ProcessMouseButton(buttonState.lastFrameDelta, eventData, buttonState.hasNativeClickCount);
            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.rightButton = buttonState;

            /// Middle Mouse Button
            buttonState = mouseState.middleButton;
            buttonState.CopyTo(eventData);
            eventData.button = PointerEventData.InputButton.Middle;

            ProcessMouseButton(buttonState.lastFrameDelta, eventData, buttonState.hasNativeClickCount);
            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.middleButton = buttonState;

            mouseState.OnFrameFinished();
        }

        // if we are using a MultiplayerEventSystem, ignore any transforms
        // not under the current MultiplayerEventSystem's root.
        private bool PointerShouldIgnoreTransform(Transform t)
        {
            if (eventSystem is MultiplayerEventSystem)
            {
                var mes = eventSystem as MultiplayerEventSystem;

                if (mes.playerRoot != null)
                    if (!t.IsChildOf(mes.playerRoot.transform))
                        return true;
            }
            return false;
        }

        private void ProcessMouseMovement(PointerEventData eventData)
        {
            var currentPointerTarget = eventData.pointerCurrentRaycast.gameObject;

            // If we have no target or pointerEnter has been deleted,
            // we just send exit events to anything we are tracking
            // and then exit.
            if (currentPointerTarget == null || eventData.pointerEnter == null)
            {
                for (var i = 0; i < eventData.hovered.Count; ++i)
                    ExecuteEvents.Execute(eventData.hovered[i], eventData, ExecuteEvents.pointerExitHandler);

                eventData.hovered.Clear();

                if (currentPointerTarget == null)
                {
                    eventData.pointerEnter = null;
                    return;
                }
            }

            if (eventData.pointerEnter == currentPointerTarget && currentPointerTarget)
                return;

            var commonRoot = FindCommonRoot(eventData.pointerEnter, currentPointerTarget);

            // We walk up the tree until a common root and the last entered and current entered object is found.
            // Then send exit and enter events up to, but not including, the common root.
            if (eventData.pointerEnter != null)
            {
                var t = eventData.pointerEnter.transform;

                while (t != null)
                {
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    ExecuteEvents.Execute(t.gameObject, eventData, ExecuteEvents.pointerExitHandler);

                    eventData.hovered.Remove(t.gameObject);

                    t = t.parent;
                }
            }

            eventData.pointerEnter = currentPointerTarget;
            if (currentPointerTarget != null)
            {
                var t = currentPointerTarget.transform;

                while (t != null && t.gameObject != commonRoot && !PointerShouldIgnoreTransform(t))
                {
                    ExecuteEvents.Execute(t.gameObject, eventData, ExecuteEvents.pointerEnterHandler);

                    eventData.hovered.Add(t.gameObject);

                    t = t.parent;
                }
            }
        }

        private void ProcessMouseButton(ButtonDeltaState mouseButtonChanges, PointerEventData eventData, bool hasNativeClickCount)
        {
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            if (currentOverGo != null && PointerShouldIgnoreTransform(currentOverGo.transform))
                return;

            if ((mouseButtonChanges & ButtonDeltaState.Pressed) != 0)
            {
                eventData.eligibleForClick = true;
                eventData.delta = Vector2.zero;
                eventData.dragging = false;
                eventData.pressPosition = eventData.position;
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

                var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);

                // If we have clicked something new, deselect the old thing
                // and leave 'selection handling' up to the press event.
                if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                    eventSystem.SetSelectedGameObject(null, eventData);

                // search for the control that will receive the press.
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

                // We didn't find a press handler, so we search for a click handler.
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                var time = Time.unscaledTime;

                if (!hasNativeClickCount)
                {
                    const float clickSpeed = 0.3f;
                    if (newPressed == eventData.lastPress && ((time - eventData.clickTime) < clickSpeed))
                        ++eventData.clickCount;
                    else
                        eventData.clickCount = 1;
                }

                eventData.clickTime = time;

                eventData.pointerPress = newPressed;
                eventData.rawPointerPress = currentOverGo;

                // Save the drag handler for drag events during this mouse down.
                eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (eventData.pointerDrag != null)
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
            }

            if ((mouseButtonChanges & ButtonDeltaState.Released) != 0)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
                {
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
                }
                else if (eventData.dragging && eventData.pointerDrag != null)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);
                }

                eventData.eligibleForClick = eventData.dragging = false;
                eventData.pointerPress = eventData.rawPointerPress = eventData.pointerDrag = null;
            }
        }

        private void ProcessMouseButtonDrag(PointerEventData eventData, float pixelDragThresholdMultiplier = 1.0f)
        {
            if (!eventData.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                eventData.pointerDrag == null)
                return;

            if (!eventData.dragging)
            {
                if ((eventData.pressPosition - eventData.position).sqrMagnitude >= ((eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold) * pixelDragThresholdMultiplier))
                {
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                    eventData.dragging = true;
                }
            }

            if (eventData.dragging)
            {
                // If we moved from our initial press object, process an up for that object.
                if (eventData.pointerPress != eventData.pointerDrag)
                {
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                    eventData.eligibleForClick = false;
                    eventData.pointerPress = null;
                    eventData.rawPointerPress = null;
                }
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
            }
        }

        private void ProcessMouseScroll(PointerEventData eventData)
        {
            var scrollDelta = eventData.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(eventData.pointerEnter);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, eventData, ExecuteEvents.scrollHandler);
            }
        }

        internal void ProcessTrackedDevice(ref TrackedDeviceModel deviceState)
        {
            if (!deviceState.changedThisFrame)
                return;

            var eventData = GetOrCreateCachedTrackedPointerEvent();
            eventData.Reset();
            deviceState.CopyTo(eventData);

            eventData.button = PointerEventData.InputButton.Left;
            eventData.pointerCurrentRaycast = PerformRaycast(eventData);

            ProcessMouseButton(deviceState.selectDelta, eventData, false);
            ProcessMouseMovement(eventData);
            ProcessMouseButtonDrag(eventData, trackedDeviceDragThresholdMultiplier);

            deviceState.CopyFrom(eventData);

            deviceState.OnFrameFinished();
        }

        /// <summary>
        /// Takes an existing JoystickModel and dispatches all relevant changes through the event system.
        /// It also updates the internal data of the JoystickModel.
        /// </summary>
        /// <param name="joystickState">The joystick state you want to forward into the UI Event System</param>
        internal void ProcessJoystick(ref JoystickModel joystickState)
        {
            var internalJoystickState = joystickState.internalData;

            var usedSelectionChange = false;
            if (eventSystem.currentSelectedGameObject != null)
            {
                var data = GetBaseEventData();
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
                usedSelectionChange = data.used;
            }

            // Don't send move events if disabled in the EventSystem.
            if (!eventSystem.sendNavigationEvents)
                return;

            var movement = joystickState.move;
            if (!usedSelectionChange && (!Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f)))
            {
                var time = Time.unscaledTime;

                var moveVector = joystickState.move;

                var moveDirection = MoveDirection.None;
                if (moveVector.sqrMagnitude > 0)
                {
                    if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
                        moveDirection = (moveVector.x > 0) ? MoveDirection.Right : MoveDirection.Left;
                    else
                        moveDirection = moveVector.y > 0 ? MoveDirection.Up : MoveDirection.Down;
                }

                if (moveDirection != internalJoystickState.lastMoveDirection)
                {
                    internalJoystickState.consecutiveMoveCount = 0;
                }

                if (moveDirection != MoveDirection.None)
                {
                    var allow = true;
                    if (internalJoystickState.consecutiveMoveCount != 0)
                    {
                        if (internalJoystickState.consecutiveMoveCount > 1)
                            allow = time > internalJoystickState.lastMoveTime + repeatRate;
                        else
                            allow = time > internalJoystickState.lastMoveTime + repeatDelay;
                    }

                    if (allow)
                    {
                        var eventData = GetOrCreateCachedAxisEvent();
                        eventData.Reset();

                        eventData.moveVector = moveVector;
                        eventData.moveDir = moveDirection;

                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.moveHandler);
                        usedSelectionChange = eventData.used;

                        internalJoystickState.consecutiveMoveCount = internalJoystickState.consecutiveMoveCount + 1;
                        internalJoystickState.lastMoveTime = time;
                        internalJoystickState.lastMoveDirection = moveDirection;
                    }
                }
                else
                    internalJoystickState.consecutiveMoveCount = 0;
            }
            else
                internalJoystickState.consecutiveMoveCount = 0;

            if (!usedSelectionChange)
            {
                if (eventSystem.currentSelectedGameObject != null)
                {
                    var data = GetBaseEventData();
                    if ((joystickState.submitButtonDelta & ButtonDeltaState.Pressed) != 0)
                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

                    if (!data.used && (joystickState.cancelButtonDelta & ButtonDeltaState.Pressed) != 0)
                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
                }
            }

            joystickState.internalData = internalJoystickState;
            joystickState.OnFrameFinished();
        }

        private PointerEventData GetOrCreateCachedPointerEvent()
        {
            var result = m_CachedPointerEvent;
            if (result == null)
            {
                result = new PointerEventData(eventSystem);
                m_CachedPointerEvent = result;
            }

            return result;
        }

        private TrackedPointerEventData GetOrCreateCachedTrackedPointerEvent()
        {
            var result = m_CachedTrackedPointerEventData;
            if (result == null)
            {
                result = new TrackedPointerEventData(eventSystem);
                m_CachedTrackedPointerEventData = result;
            }

            return result;
        }

        private AxisEventData GetOrCreateCachedAxisEvent()
        {
            var result = m_CachedAxisEvent;
            if (result == null)
            {
                result = new AxisEventData(eventSystem);
                m_CachedAxisEvent = result;
            }

            return result;
        }

        [Tooltip("The Initial delay (in seconds) between an initial move action and a repeated move action.")]
        [SerializeField]
        private float m_RepeatDelay = 0.5f;

        [Tooltip("The speed (in seconds) that the move action repeats itself once repeating.")]
        [SerializeField]
        private float m_RepeatRate = 0.1f;

        [Tooltip("Scales the Eventsystem.DragThreshold, for tracked devices, to make selection easier.")]
        // Hide this while we still have to figure out what to do with this.
        private float m_TrackedDeviceDragThresholdMultiplier = 2.0f;

        private AxisEventData m_CachedAxisEvent;
        private PointerEventData m_CachedPointerEvent;
        private TrackedPointerEventData m_CachedTrackedPointerEventData;

        public float repeatDelay
        {
            get { return m_RepeatDelay; }
            set { m_RepeatDelay = value; }
        }

        public float repeatRate
        {
            get { return m_RepeatRate; }
            set { m_RepeatRate = value; }
        }

        public float trackedDeviceDragThresholdMultiplier
        {
            get { return m_TrackedDeviceDragThresholdMultiplier; }
            set { m_TrackedDeviceDragThresholdMultiplier = value; }
        }

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

        int GetMouseDeviceIndexForCallbackContext(InputAction.CallbackContext context)
        {
            Debug.Assert(context.action.type == InputActionType.PassThrough, $"Pointer actions should be pass-through actions, so the UI can properly distinguish multiple pointing devices/fingers. Please set the action type of '{context.action.name}' to 'Pass-Through'.");
            var touchId = 0;
            if (context.control.parent is TouchControl)
                touchId = ((TouchControl)context.control.parent).touchId.ReadValue();

            for (var i = 0; i < mouseStates.Count; i++)
            {
                if (mouseStates[i].device == context.control.device && mouseStates[i].touchId == touchId)
                    return i;
            }
            mouseStates.Add(new MouseModel(m_RollingPointerId++, context.control.device, touchId));
            return mouseStates.Count - 1;
        }

        void OnAction(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == m_PointAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = mouseStates[index];
                state.position = context.ReadValue<Vector2>();
                mouseStates[index] = state;
            }
            else if (action == m_ScrollWheelAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = mouseStates[index];
                // The old input system reported scroll deltas in lines, we report pixels.
                // Need to scale as the UI system expects lines.
                const float kPixelPerLine = 20;
                state.scrollDelta = context.ReadValue<Vector2>() * (1.0f / kPixelPerLine);
                mouseStates[index] = state;
            }
            else if (action == m_LeftClickAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = mouseStates[index];

                var buttonState = state.leftButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                state.leftButton = buttonState;
                mouseStates[index] = state;
            }
            else if (action == m_RightClickAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = mouseStates[index];

                var buttonState = state.rightButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                state.rightButton = buttonState;
                mouseStates[index] = state;
            }
            else if (action == m_MiddleClickAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = mouseStates[index];

                var buttonState = state.middleButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                state.middleButton = buttonState;
                mouseStates[index] = state;
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
                foreach (var mouseState in mouseStates)
                    mouseState.OnFrameFinished();
                foreach (var trackedDeviceState in trackedDeviceStates)
                    trackedDeviceState.OnFrameFinished();
            }
            else
            {
                ProcessJoystick(ref joystickState);

                for (var i = 0; i < mouseStates.Count; i++)
                {
                    var state = mouseStates[i];
                    ProcessMouse(ref state);
                    mouseStates[i] = state;
                }

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

        [NonSerialized] private JoystickModel joystickState;
        [NonSerialized] private List<TrackedDeviceModel> trackedDeviceStates = new List<TrackedDeviceModel>();
        [NonSerialized] private List<MouseModel> mouseStates = new List<MouseModel>();
    }
}
