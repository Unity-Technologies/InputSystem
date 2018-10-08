using System;
using UnityEngine.EventSystems;

////REVIEW: apparently EventSystem only supports a single "current" module so the approach here probably
////        won't fly and we'll have to roll all non-action modules into one big module

namespace UnityEngine.Experimental.Input.Plugins.UI
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
        private RaycastResult PerformRaycast(PointerEventData eventData)
        {
            if (eventData == null)
                throw new ArgumentNullException("eventData");

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            RaycastResult result = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
            return result;
        }

        private PointerEventData PreparePointerEventData(MouseModel mouseState)
        {
            PointerEventData eventData = GetOrCreateCachedPointerEvent();
            eventData.Reset();

            eventData.pointerId = mouseState.pointerId;
            eventData.position = mouseState.position;
            eventData.delta = mouseState.deltaPosition;
            eventData.scrollDelta = mouseState.scrollDelta;
            eventData.pointerEnter = mouseState.internalData.pointerTarget;

            // This is unset in legacy systems and can safely assumed to stay true.
            eventData.useDragThreshold = true;

            eventData.pointerCurrentRaycast = PerformRaycast(eventData);

            return eventData;
        }

        /// <summary>
        /// Takes an existing MouseModel and dispatches all relevant changes through the event system.
        /// It also updates the internal data of the MouseModel.
        /// </summary>
        /// <param name="mouseState">The mouse state you want to forward into the UI Event System</param>
        protected void ProcessMouse(ref MouseModel mouseState)
        {
            if (!mouseState.changedThisFrame)
                return;

            var eventData = PreparePointerEventData(mouseState);

            /// Left Mouse Button
            // The left mouse button is 'dominant' and we want to also process hover and scroll events as if the occurred during the left click.
            var buttonState = mouseState.leftButton;
            buttonState.CopyTo(eventData);
            ProcessMouseButton(buttonState.lastFrameDelta, eventData);

            ProcessMouseMovement(eventData);
            ProcessMouseScroll(eventData);

            MouseModel.InternalData internalMouseData = mouseState.internalData;
            internalMouseData.hoverTargets = eventData.hovered;
            internalMouseData.pointerTarget = eventData.pointerEnter;
            mouseState.internalData = internalMouseData;

            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.leftButton = buttonState;

            /// Right Mouse Button
            buttonState = mouseState.rightButton;
            buttonState.CopyTo(eventData);

            ProcessMouseButton(buttonState.lastFrameDelta, eventData);
            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.rightButton = buttonState;

            /// Middle Mouse Button
            buttonState = mouseState.middleButton;
            buttonState.CopyTo(eventData);

            ProcessMouseButton(buttonState.lastFrameDelta, eventData);
            ProcessMouseButtonDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.middleButton = buttonState;

            mouseState.OnFrameFinished();
        }

        private void ProcessMouseMovement(PointerEventData eventData)
        {
            // walk up the tree till a common root between the last entered and the current entered is foung
            // send exit events up to (but not inluding) the common root. Then send enter events up to
            // (but not including the common root).

            GameObject currentPointerTarget = eventData.pointerCurrentRaycast.gameObject;

            // if we have no target / pointerEnter has been deleted
            // just send exit events to anything we are tracking
            // then exit
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

            // if we have not changed hover target
            if (eventData.pointerEnter == currentPointerTarget && currentPointerTarget)
                return;

            var commonRoot = FindCommonRoot(eventData.pointerEnter, currentPointerTarget);

            // and we already an entered object from last time
            if (eventData.pointerEnter != null)
            {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                var t = eventData.pointerEnter.transform;

                while (t != null)
                {
                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    ExecuteEvents.Execute(t.gameObject, eventData, ExecuteEvents.pointerExitHandler);

                    eventData.hovered.Remove(t.gameObject);

                    t = t.parent;
                }
            }

            // now issue the enter call up to but not including the common root
            eventData.pointerEnter = currentPointerTarget;
            if (currentPointerTarget != null)
            {
                var t = currentPointerTarget.transform;

                while (t != null && t.gameObject != commonRoot)
                {
                    ExecuteEvents.Execute(t.gameObject, eventData, ExecuteEvents.pointerEnterHandler);

                    eventData.hovered.Add(t.gameObject);

                    t = t.parent;
                }
            }
        }

        private void ProcessMouseButton(ButtonDeltaState mouseButtonChanges, PointerEventData eventData)
        {
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            if ((mouseButtonChanges & ButtonDeltaState.Pressed) != 0)
            {
                eventData.eligibleForClick = true;
                eventData.delta = Vector2.zero; //TODO TB: The Delta still exists, why are we wiping it out.
                eventData.dragging = false;
                eventData.pressPosition = eventData.position;
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

                // Selection tracking
                var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
                // if we have clicked something new, deselect the old thing
                // leave 'selection handling' up to the press event though.
                if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                    eventSystem.SetSelectedGameObject(null, eventData);

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                if (newPressed == eventData.lastPress && ((time - eventData.clickTime) < clickSpeed))
                    ++eventData.clickCount;
                else
                    eventData.clickCount = 1;

                eventData.clickTime = time;

                eventData.pointerPress = newPressed;
                eventData.rawPointerPress = currentOverGo;

                // Save the drag handler as well
                eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (eventData.pointerDrag != null)
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
            }

            if ((mouseButtonChanges & ButtonDeltaState.Released) != 0)
            {
                //Check for Events
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

                //Clear Data
                eventData.eligibleForClick = eventData.dragging = false;
                eventData.pointerPress = eventData.rawPointerPress = eventData.pointerDrag = null;
            }
        }

        private void ProcessMouseButtonDrag(PointerEventData eventData)
        {
            if (!eventData.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                eventData.pointerDrag == null)
                return;

            if (!eventData.dragging)
            {
                if ((eventData.pressPosition - eventData.position).sqrMagnitude >= (eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold))
                {
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                    eventData.dragging = true;
                }
            }

            if (eventData.dragging)
            {
                // If we moved from our initial press object
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

        /// <summary>
        /// Takes an existing JoystickModel and dispatches all relevant changes through the event system.
        /// It also updates the internal data of the JoystickModel.
        /// </summary>
        /// <param name="joystickState">The joystick state you want to forward into the UI Event System</param>
        protected void ProcessJoystick(ref JoystickModel joystickState)
        {
            JoystickModel.InternalData internalJoystickState = joystickState.internalData;

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

            Vector2 movement = joystickState.move;
            if (!usedSelectionChange && (!Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f)))
            {
                float time = Time.unscaledTime;

                Vector2 moveVector = joystickState.move;

                MoveDirection moveDirection = MoveDirection.None;
                if (moveVector.sqrMagnitude > moveDeadzone * moveDeadzone)
                {
                    if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
                        moveDirection = (moveVector.x > 0) ? MoveDirection.Right : MoveDirection.Left;
                    else
                        moveDirection = (moveVector.y > 0) ? MoveDirection.Up : MoveDirection.Down;
                }

                if (moveDirection != internalJoystickState.lastMoveDirection)
                {
                    internalJoystickState.consecutiveMoveCount = 0;
                }

                if (moveDirection != MoveDirection.None)
                {
                    bool allow = true;
                    if (internalJoystickState.consecutiveMoveCount != 0)
                    {
                        if (internalJoystickState.consecutiveMoveCount > 1)
                            allow = (time > (internalJoystickState.lastMoveTime + repeatRate));
                        else
                            allow = (time > (internalJoystickState.lastMoveTime + repeatDelay));
                    }

                    if (allow)
                    {
                        AxisEventData eventData = GetOrCreateCachedAxisEvent();
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

        protected PointerEventData GetOrCreateCachedPointerEvent()
        {
            var result = m_CachedPointerEvent;
            if (result == null)
            {
                result = new PointerEventData(eventSystem);
                m_CachedPointerEvent = result;
            }

            return result;
        }

        protected AxisEventData GetOrCreateCachedAxisEvent()
        {
            var result = m_CachedAxisEvent;
            if (result == null)
            {
                result = new AxisEventData(eventSystem);
                m_CachedAxisEvent = result;
            }

            return result;
        }

        [Tooltip("The maximum time (in seconds) between two mouse presses for it to be consecutive click.")]
        public float clickSpeed = 0.3f;

        [Tooltip("The absolute value required by a move action on either axis required to trigger a move event.")]
        public float moveDeadzone = 0.6f;

        [Tooltip("The Initial delay (in seconds) between an initial move action and a repeated move action.")]
        public float repeatDelay = 0.5f;

        [Tooltip("The speed (in seconds) that the move action repeats itself once repeating.")]
        public float repeatRate = 0.1f;

        private AxisEventData m_CachedAxisEvent;
        private PointerEventData m_CachedPointerEvent;
    }
}
