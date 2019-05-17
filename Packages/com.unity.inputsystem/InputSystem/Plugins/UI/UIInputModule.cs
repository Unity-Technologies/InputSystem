using System;
using UnityEngine.EventSystems;

////REVIEW: apparently EventSystem only supports a single "current" module so the approach here probably
////        won't fly and we'll have to roll all non-action modules into one big module

namespace UnityEngine.InputSystem.Plugins.UI
{
    /// <summary>
    /// Base class for <see cref="BaseInputModule">input modules</see> that send
    /// UI input.
    /// </summary>
    /// <remarks>
    /// Multiple input modules may be placed on the same event system. In such a setup,
    /// the modules will synchronize with each other to not send
    /// </remarks>
    public abstract class UIInputModule : BaseInputModule
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

        internal void ProcessTouch(ref TouchModel touchState)
        {
            if (!touchState.changedThisFrame)
                return;

            var eventData = GetOrCreateCachedPointerEvent();
            eventData.Reset();

            touchState.CopyTo(eventData);

            if (touchState.selectPhase == PointerPhase.Canceled)
            {
                eventData.pointerCurrentRaycast = (touchState.selectPhase == PointerPhase.Canceled) ? new RaycastResult() : PerformRaycast(eventData);
            }
            else
            {
                eventData.pointerCurrentRaycast = PerformRaycast(eventData);
            }

            eventData.button = PointerEventData.InputButton.Left;

            ProcessMouseButton(touchState.selectDelta, eventData, false);
            ProcessMouseMovement(eventData);
            ProcessMouseButtonDrag(eventData);

            touchState.CopyFrom(eventData);

            touchState.OnFrameFinished();
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
    }
}
