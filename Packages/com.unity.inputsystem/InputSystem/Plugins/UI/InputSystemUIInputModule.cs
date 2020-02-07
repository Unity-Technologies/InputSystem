using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

////REVIEW: should each of the actions be *lists* of actions?

////FIXME: doesn't handle devices getting removed; will just keep device states forever

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
        /// <summary>
        /// Whether to clear the current selection when a click happens that does not hit any <c>GameObject</c>.
        /// </summary>
        /// <value>If true (default), clicking outside of any GameObject will reset the current selection.</value>
        /// <remarks>
        /// By toggling this behavior off, background clicks will keep the current selection. I.e.
        /// <c>EventSystem.currentSelectedGameObject</c> will not be changed.
        /// </remarks>
        public bool deselectOnBackgroundClick
        {
            get => m_DeselectOnBackgroundClick;
            set => m_DeselectOnBackgroundClick = value;
        }

        public override void ActivateModule()
        {
            base.ActivateModule();

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        public override bool IsPointerOverGameObject(int pointerId)
        {
            foreach (var state in m_MouseStates)
            {
                if (state.touchId == pointerId)
                    return state.pointerTarget != null;
            }
            return false;
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
        private void ProcessMouse(ref MouseModel mouseState)
        {
            if (!mouseState.changedThisFrame)
                return;

            var eventData = GetOrCreateCachedPointerEvent();
            eventData.Reset();

            mouseState.CopyTo(eventData);

            eventData.pointerCurrentRaycast = PerformRaycast(eventData);

            // Left Mouse Button
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

            // Right Mouse Button
            buttonState = mouseState.rightButton;
            buttonState.CopyTo(eventData);
            eventData.button = PointerEventData.InputButton.Right;

            ProcessMouseButton(buttonState.lastFrameDelta, eventData, buttonState.hasNativeClickCount);
            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.rightButton = buttonState;

            // Middle Mouse Button
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
            if (eventSystem is MultiplayerEventSystem multiplayerEventSystem && multiplayerEventSystem.playerRoot != null)
            {
                if (!t.IsChildOf(multiplayerEventSystem.playerRoot.transform))
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

                // If we have clicked something new, deselect the old thing and leave 'selection handling' up
                // to the press event (except if there's none and we're told to not deselect in that case).
                if (selectHandlerGO != eventSystem.currentSelectedGameObject && (selectHandlerGO != null || m_DeselectOnBackgroundClick))
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

        private static void ProcessMouseScroll(PointerEventData eventData)
        {
            var scrollDelta = eventData.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(eventData.pointerEnter);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, eventData, ExecuteEvents.scrollHandler);
            }
        }

        private void ProcessTrackedDevice(ref TrackedDeviceModel deviceState)
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
            get => m_RepeatDelay;
            set => m_RepeatDelay = value;
        }

        public float repeatRate
        {
            get => m_RepeatRate;
            set => m_RepeatRate = value;
        }

        public float trackedDeviceDragThresholdMultiplier
        {
            get => m_TrackedDeviceDragThresholdMultiplier;
            set => m_TrackedDeviceDragThresholdMultiplier = value;
        }

        private void SwapAction(ref InputActionReference property, InputActionReference newValue, bool actionsHooked, Action<InputAction.CallbackContext> actionCallback)
        {
            if (property == newValue || (property != null && newValue != null && property.action == newValue.action))
                return;

            if (property != null && actionsHooked)
            {
                property.action.performed -= actionCallback;
                property.action.canceled -= actionCallback;
            }

            property = newValue;

            #if DEBUG
            if (newValue != null && newValue.action != null && newValue.action.type != InputActionType.PassThrough)
            {
                Debug.LogWarning("Actions used with the UI input module should generally be set to Pass-Through type so that the module can properly distinguish between "
                    + $"input from multiple devices (action {newValue.action} is set to {newValue.action.type})", this);
            }
            #endif

            if (newValue != null && actionsHooked)
            {
                property.action.performed += actionCallback;
                property.action.canceled += actionCallback;
            }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2"/> 2D screen position
        /// used as a cursor for pointing at UI elements.
        /// </summary>
        public InputActionReference point
        {
            get => m_PointAction;
            set => SwapAction(ref m_PointAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector2</c> 2D motion vector
        /// used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        public InputActionReference move
        {
            get => m_MoveAction;
            set => SwapAction(ref m_MoveAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector2</c> scroll wheel value
        /// used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference scrollWheel
        {
            get => m_ScrollWheelAction;
            set => SwapAction(ref m_ScrollWheelAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>bool</c> button value
        /// used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference leftClick
        {
            get => m_LeftClickAction;
            set => SwapAction(ref m_LeftClickAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>bool</c> button value
        /// used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference middleClick
        {
            get => m_MiddleClickAction;
            set => SwapAction(ref m_MiddleClickAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>bool"</c> button value
        /// used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        public InputActionReference rightClick
        {
            get => m_RightClickAction;
            set => SwapAction(ref m_RightClickAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>bool</c> button value
        /// used for sending <see cref="BaseEventData"/> events.
        /// </summary>
        public InputActionReference submit
        {
            get => m_SubmitAction;
            set => SwapAction(ref m_SubmitAction, value, m_ActionsHooked, OnAction);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>bool</c> button value
        /// used for sending <see cref="BaseEventData"/> events.
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
            m_JoystickState.Reset();
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

        private bool IsAnyActionEnabled()
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

        private int GetTrackedDeviceIndexForCallbackContext(InputAction.CallbackContext context)
        {
            for (var i = 0; i < m_TrackedDeviceStatesCount; i++)
            {
                if (m_TrackedDeviceStates[i].device == context.control.device)
                    return i;
            }

            return ArrayHelpers.AppendWithCapacity(ref m_TrackedDeviceStates, ref m_TrackedDeviceStatesCount,
                new TrackedDeviceModel(m_RollingPointerId++, context.control.device));
        }

        private int GetMouseDeviceIndexForCallbackContext(InputAction.CallbackContext context)
        {
            var touchId = PointerInputModule.kMouseLeftId;
            if (context.control.parent is TouchControl touchControl)
                touchId = touchControl.touchId.ReadValue();

            for (var i = 0; i < m_MouseStates.length; i++)
            {
                if (m_MouseStates[i].device == context.control.device && m_MouseStates[i].touchId == touchId)
                    return i;
            }

            return m_MouseStates.AppendWithCapacity(new MouseModel(m_RollingPointerId++, context.control.device, touchId));
        }

        private void OnAction(InputAction.CallbackContext context)
        {
            var action = context.action;
            if (action == m_PointAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = m_MouseStates[index];
                state.position = context.ReadValue<Vector2>();
                m_MouseStates[index] = state;
            }
            else if (action == m_ScrollWheelAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = m_MouseStates[index];
                // The old input system reported scroll deltas in lines, we report pixels.
                // Need to scale as the UI system expects lines.
                const float kPixelPerLine = 20;
                state.scrollDelta = context.ReadValue<Vector2>() * (1.0f / kPixelPerLine);
                m_MouseStates[index] = state;
            }
            ////FIXME: these are missing clicks that happen within the same frame :(
            ////       (for these actions, perform polling rather than doing the thing here)
            else if (action == m_LeftClickAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = m_MouseStates[index];

                var buttonState = state.leftButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                state.leftButton = buttonState;
                m_MouseStates[index] = state;
            }
            else if (action == m_RightClickAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = m_MouseStates[index];

                var buttonState = state.rightButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                state.rightButton = buttonState;
                m_MouseStates[index] = state;
            }
            else if (action == m_MiddleClickAction?.action)
            {
                var index = GetMouseDeviceIndexForCallbackContext(context);
                var state = m_MouseStates[index];

                var buttonState = state.middleButton;
                buttonState.isDown = context.ReadValue<float>() > 0;
                buttonState.clickCount = (context.control.device as Mouse)?.clickCount.ReadValue() ?? 0;
                state.middleButton = buttonState;
                m_MouseStates[index] = state;
            }
            else if (action == m_MoveAction?.action)
            {
                m_JoystickState.move = context.ReadValue<Vector2>();
            }
            else if (action == m_SubmitAction?.action)
            {
                m_JoystickState.submitButtonDown = context.ReadValue<float>() > 0;
            }
            else if (action == m_CancelAction?.action)
            {
                m_JoystickState.cancelButtonDown = context.ReadValue<float>() > 0;
            }
            else if (action == m_TrackedDeviceOrientationAction?.action)
            {
                var index = GetTrackedDeviceIndexForCallbackContext(context);
                var state = m_TrackedDeviceStates[index];
                state.orientation = context.ReadValue<Quaternion>();
                m_TrackedDeviceStates[index] = state;
            }
            else if (action == m_TrackedDevicePositionAction?.action)
            {
                var index = GetTrackedDeviceIndexForCallbackContext(context);
                var state = m_TrackedDeviceStates[index];
                state.position = context.ReadValue<Vector3>();
                m_TrackedDeviceStates[index] = state;
            }
            else if (action == m_TrackedDeviceSelectAction?.action)
            {
                var index = GetTrackedDeviceIndexForCallbackContext(context);
                var state = m_TrackedDeviceStates[index];
                state.select = context.ReadValue<float>() > 0;
                m_TrackedDeviceStates[index] = state;
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
                m_JoystickState.OnFrameFinished();
                for (var i = 0; i < m_MouseStates.length; ++i)
                    m_MouseStates[i].OnFrameFinished();
                for (var i = 0; i < m_TrackedDeviceStatesCount; ++i)
                    m_TrackedDeviceStates[i].OnFrameFinished();
            }
            else
            {
                ProcessJoystick(ref m_JoystickState);

                for (var i = 0; i < m_MouseStates.length; i++)
                {
                    var state = m_MouseStates[i];
                    ProcessMouse(ref state);
                    m_MouseStates[i] = state;
                }

                for (var i = 0; i < m_TrackedDeviceStatesCount; i++)
                {
                    var state = m_TrackedDeviceStates[i];
                    ProcessTrackedDevice(ref state);
                    m_TrackedDeviceStates[i] = state;
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

            SetActionCallback(m_PointAction, true);
            SetActionCallback(m_MoveAction, true);
            SetActionCallback(m_LeftClickAction, true);
            SetActionCallback(m_RightClickAction, true);
            SetActionCallback(m_MiddleClickAction, true);
            SetActionCallback(m_SubmitAction, true);
            SetActionCallback(m_CancelAction, true);
            SetActionCallback(m_ScrollWheelAction, true);
            SetActionCallback(m_TrackedDeviceOrientationAction, true);
            SetActionCallback(m_TrackedDevicePositionAction, true);
            SetActionCallback(m_TrackedDeviceSelectAction, true);
        }

        private void UnhookActions()
        {
            if (!m_ActionsHooked)
                return;

            m_ActionsHooked = false;

            SetActionCallback(m_PointAction, false);
            SetActionCallback(m_MoveAction, false);
            SetActionCallback(m_LeftClickAction, false);
            SetActionCallback(m_RightClickAction, false);
            SetActionCallback(m_MiddleClickAction, false);
            SetActionCallback(m_SubmitAction, false);
            SetActionCallback(m_CancelAction, false);
            SetActionCallback(m_ScrollWheelAction, false);
            SetActionCallback(m_TrackedDeviceOrientationAction, false);
            SetActionCallback(m_TrackedDevicePositionAction, false);
            SetActionCallback(m_TrackedDeviceSelectAction, false);
        }

        private void SetActionCallback(InputActionReference actionReference, bool install)
        {
            if (actionReference == null)
                return;

            var action = actionReference.action;
            if (action == null)
                return;

            if (install)
            {
                action.performed += m_OnActionDelegate;
                action.canceled += m_OnActionDelegate;
            }
            else
            {
                action.performed -= m_OnActionDelegate;
                action.canceled -= m_OnActionDelegate;
            }
        }

        private InputActionReference UpdateReferenceForNewAsset(InputActionReference actionReference)
        {
            var oldAction = actionReference?.action;
            if (oldAction == null)
                return null;

            var oldActionMap = oldAction.actionMap;
            Debug.Assert(oldActionMap != null, "Not expected to end up with a singleton action here");

            var newActionMap = m_ActionsAsset.FindActionMap(oldActionMap.name);
            if (newActionMap == null)
                return null;

            var newAction = newActionMap.FindAction(oldAction.name);
            if (newAction == null)
                return null;

            return InputActionReference.Create(newAction);
        }

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

        [SerializeField, HideInInspector] private InputActionReference m_PointAction;
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

        [SerializeField, HideInInspector] private InputActionAsset m_ActionsAsset;

        [SerializeField] private bool m_DeselectOnBackgroundClick = true;

        private int m_RollingPointerId;
        private bool m_OwnsEnabledState;
        private bool m_ActionsHooked;
        private Action<InputAction.CallbackContext> m_OnActionDelegate;

        private JoystickModel m_JoystickState;
        private int m_TrackedDeviceStatesCount;
        private TrackedDeviceModel[] m_TrackedDeviceStates;
        private InlinedArray<MouseModel> m_MouseStates;
    }
}
