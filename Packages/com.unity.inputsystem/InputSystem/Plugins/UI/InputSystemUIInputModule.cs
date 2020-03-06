using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

////FIXME: The UI is currently not reacting to pointers until they are moved after the UI module has been enabled. What needs to
////       happen is that point, trackedDevicePosition, and trackedDeviceOrientation have initial state checks. However, for touch,
////       we do *not* want to react to the initial value as then we also get presses (unlike with other pointers). Argh.

////REVIEW: I think this would be much better served by having a composite type input for each of the three basic types of input (pointer, navigation, tracked)
////        I.e. there'd be a PointerInput, a NavigationInput, and a TrackedInput composite. This would solve several problems in one go and make
////        it much more obvious which inputs go together.

////REVIEW: The current input model has too much complexity for pointer input; find a way to simplify this.

////REVIEW: how does this/uGUI support drag-scrolls on touch? [GESTURES]

////REVIEW: how does this/uGUI support two-finger right-clicks with touch? [GESTURES]

////TODO: add ability to query which device was last used with any of the actions

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

        /// <summary>
        /// How to deal with the presence of pointer-type input from multiple devices.
        /// </summary>
        /// <value>Wh </value>
        public UIPointerBehavior pointerBehavior
        {
            get => m_PointerBehavior;
            set => m_PointerBehavior = value;
        }

        /// <summary>
        /// Called by <c>EventSystem</c> when the input module is made current.
        /// </summary>
        public override void ActivateModule()
        {
            base.ActivateModule();

            // Select firstSelectedGameObject if nothing is selected ATM.
            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;
            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /// <summary>
        /// Check whether the given pointer or touch is currently hovering over a <c>GameObject</c>.
        /// </summary>
        /// <param name="pointerOrTouchId">ID of the pointer or touch. Meaning this should correspond to either
        /// <c>PointerEventData.pointerId</c> or <see cref="ExtendedPointerEventData.touchId"/>. The pointer ID
        /// generally corresponds to the <see cref="InputDevice.deviceId"/> of the pointer device. An exception
        /// to this are touches as a <see cref="Touchscreen"/> may have multiple pointers (one for each active
        /// finger). For touch, you can use the <see cref="TouchControl.touchId"/> of the touch.
        ///
        /// To check whether any pointer is over a <c>GameObject</c>, simply pass a negative value such as -1.</param>
        /// <returns>True if the given pointer is currently hovering over a <c>GameObject</c>.</returns>
        /// <remarks>
        /// The result is true if the given pointer has caused an <c>IPointerEnter</c> event to be sent to a
        /// <c>GameObject</c>.
        ///
        /// This method can be invoked via <c>EventSystem.current.IsPointerOverGameObject</c>.
        ///
        /// <example>
        /// <code>
        /// // In general, the pointer ID corresponds to the device ID:
        /// EventSystem.current.IsPointerOverGameObject(XRController.leftHand.deviceId);
        /// EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
        ///
        /// // For touch input, pass the ID of a touch:
        /// EventSystem.current.IsPointerOverGameObject(Touchscreen.primaryTouch.touchId.ReadValue());
        ///
        /// // But can also pass the ID of the entire Touchscreen in which case the result
        /// // is true if any touch is over a GameObject:
        /// EventSystem.current.IsPointerOverGameObject(Touchscreen.current.deviceId);
        ///
        /// // Finally, any negative value will be interpreted as "any pointer" and will
        /// // return true if any one pointer is currently over a GameObject:
        /// EventSystem.current.IsPointerOverGameObject(-1);
        /// EventSystem.current.IsPointerOverGameObject(); // Equivalent.
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ExtendedPointerEventData.touchId"/>
        /// <seealso cref="InputDevice.deviceId"/>
        public override bool IsPointerOverGameObject(int pointerOrTouchId)
        {
            var stateIndex = -1;

            if (pointerOrTouchId < 0)
            {
                if (m_CurrentPointerId != -1)
                {
                    stateIndex = m_CurrentPointerIndex;
                }
                else
                {
                    // No current pointer. Can happen, for example, when a touch just ended and its pointer record
                    // was removed as a result. If we still have some active pointer, use it.
                    if (m_PointerStates.length > 0)
                        stateIndex = 0;
                }
            }
            else
            {
                stateIndex = GetPointerStateIndexFor(pointerOrTouchId);

                if (stateIndex == -1)
                {
                    for (var i = 0; i < m_PointerStates.length; ++i)
                    {
                        var eventData = m_PointerStates[i].eventData;
                        if (eventData.touchId == pointerOrTouchId || (eventData.touchId != 0 && eventData.device.deviceId == pointerOrTouchId))
                            return eventData.pointerEnter != null;
                    }
                }
            }
            if (stateIndex == -1)
                return false;

            return m_PointerStates[stateIndex].eventData.pointerEnter != null;
        }

        private RaycastResult PerformRaycast(ExtendedPointerEventData eventData)
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            // If it's an event from a tracked device, see if we have a TrackedDeviceRaycaster and give it
            // the first shot.
            if (eventData.pointerType == UIPointerType.Tracked && TrackedDeviceRaycaster.s_Instances.length > 0)
            {
                for (var i = 0; i < TrackedDeviceRaycaster.s_Instances.length; ++i)
                {
                    var trackedDeviceRaycaster = TrackedDeviceRaycaster.s_Instances[i];
                    m_RaycastResultCache.Clear();
                    trackedDeviceRaycaster.PerformRaycast(eventData, m_RaycastResultCache);
                    if (m_RaycastResultCache.Count > 0)
                    {
                        var raycastResult = m_RaycastResultCache[0];
                        m_RaycastResultCache.Clear();
                        return raycastResult;
                    }
                }
                return default;
            }

            // Otherwise pass it along to the normal raycasting logic.
            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            var result = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
            return result;
        }

        // Mouse, pen, touch, and tracked device pointer input all go through here.
        private void ProcessPointer(ref PointerModel state)
        {
            if (!state.changedThisFrame)
                return;

            var eventData = state.eventData;

            // Sync position.
            var pointerType = eventData.pointerType;
            if (pointerType == UIPointerType.MouseOrPen && Cursor.lockState == CursorLockMode.Locked)
            {
                eventData.position = new Vector2(-1, -1);
                ////REVIEW: This is consistent with StandaloneInputModule but having no deltas in locked mode seems wrong
                eventData.delta = default;
            }
            else if (pointerType == UIPointerType.Tracked)
            {
                eventData.trackedDeviceOrientation = state.worldOrientation;
                eventData.trackedDevicePosition = state.worldPosition;
            }
            else
            {
                eventData.delta = state.screenPosition - eventData.position;
                eventData.position = state.screenPosition;
            }

            // Clear the 'used' flag.
            eventData.Reset();

            // Raycast from current position.
            eventData.pointerCurrentRaycast = PerformRaycast(eventData);

            // Sync position for tracking devices. For those, we can only do this
            // after the raycast as the screen-space position is a byproduct of the raycast.
            if (pointerType == UIPointerType.Tracked && eventData.pointerCurrentRaycast.isValid)
            {
                var screenPos = eventData.pointerCurrentRaycast.screenPosition;
                eventData.delta = screenPos - eventData.position;
                eventData.position = eventData.pointerCurrentRaycast.screenPosition;
            }

            ////REVIEW: for touch, we only need the left button; should we skip right and middle button processing? then we also don't need to copy to/from the event

            // Left mouse button. Movement and scrolling is processed with event set left button.
            eventData.button = PointerEventData.InputButton.Left;
            state.leftButton.CopyPressStateTo(eventData);

            ProcessPointerMovement(ref state, eventData);
            ProcessPointerButton(ref state.leftButton, eventData);
            ProcessPointerButtonDrag(ref state.leftButton, eventData);
            ProcessPointerScroll(ref state, eventData);

            // Right mouse button.
            eventData.button = PointerEventData.InputButton.Right;
            state.rightButton.CopyPressStateTo(eventData);

            ProcessPointerButton(ref state.rightButton, eventData);
            ProcessPointerButtonDrag(ref state.rightButton, eventData);

            // Middle mouse button.
            eventData.button = PointerEventData.InputButton.Middle;
            state.middleButton.CopyPressStateTo(eventData);

            ProcessPointerButton(ref state.middleButton, eventData);
            ProcessPointerButtonDrag(ref state.middleButton, eventData);

            state.OnFrameFinished();
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

        private void ProcessPointerMovement(ref PointerModel pointer, ExtendedPointerEventData eventData)
        {
            var currentPointerTarget =
                // If the pointer is a touch that was released this frame, we generate pointer-exit events
                // and then later remove the pointer.
                (eventData.pointerType == UIPointerType.Touch && pointer.leftButton.wasReleasedThisFrame) ||
                (eventData.pointerType == UIPointerType.MouseOrPen && Cursor.lockState == CursorLockMode.Locked)
                ? null
                : eventData.pointerCurrentRaycast.gameObject;

            ProcessPointerMovement(eventData, currentPointerTarget);
        }

        private void ProcessPointerMovement(ExtendedPointerEventData eventData, GameObject currentPointerTarget)
        {
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

            var commonRoot = FindCommonRoot(eventData.pointerEnter, currentPointerTarget)?.transform;

            // We walk up the tree until a common root and the last entered and current entered object is found.
            // Then send exit and enter events up to, but not including, the common root.
            if (eventData.pointerEnter != null)
            {
                for (var current = eventData.pointerEnter.transform; current != null && current != commonRoot; current = current.parent)
                {
                    ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerExitHandler);
                    eventData.hovered.Remove(current.gameObject);
                }
            }

            eventData.pointerEnter = currentPointerTarget;
            if (currentPointerTarget != null)
            {
                for (var current = currentPointerTarget.transform;
                     current != null && current != commonRoot && !PointerShouldIgnoreTransform(current);
                     current = current.parent)
                {
                    ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
                    eventData.hovered.Add(current.gameObject);
                }
            }
        }

        private void ProcessPointerButton(ref PointerModel.ButtonState button, PointerEventData eventData)
        {
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            if (currentOverGo != null && PointerShouldIgnoreTransform(currentOverGo.transform))
                return;

            // Button press.
            if (button.wasPressedThisFrame)
            {
                eventData.delta = Vector2.zero;
                eventData.dragging = false;
                eventData.pressPosition = eventData.position;
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                eventData.eligibleForClick = true;

                var selectHandler = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);

                // If we have clicked something new, deselect the old thing and leave 'selection handling' up
                // to the press event (except if there's none and we're told to not deselect in that case).
                if (selectHandler != eventSystem.currentSelectedGameObject && (selectHandler != null || m_DeselectOnBackgroundClick))
                    eventSystem.SetSelectedGameObject(null, eventData);

                // Invoke OnPointerDown, if present.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

                // Detect clicks.
                // NOTE: StandaloneInputModule does this *after* the click handler has been invoked -- which doesn't seem to
                //       make sense. We do it *before* IPointerClickHandler.
                var time = InputRuntime.s_Instance.unscaledGameTime;
                const float clickSpeed = 0.3f;
                if (newPressed == eventData.lastPress && (time - eventData.clickTime) < clickSpeed)
                    ++eventData.clickCount;
                else
                    eventData.clickCount = 1;

                eventData.clickTime = time;

                // We didn't find a press handler, so we turn it into a click.
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                eventData.pointerPress = newPressed;
                eventData.rawPointerPress = currentOverGo;

                // Save the drag handler for drag events during this mouse down.
                eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (eventData.pointerDrag != null)
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
            }

            // Button release.
            if (button.wasReleasedThisFrame)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
                else if (eventData.dragging && eventData.pointerDrag != null)
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);

                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;

                if (eventData.dragging && eventData.pointerDrag != null)
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

                eventData.dragging = false;
                eventData.pointerDrag = null;
            }

            button.CopyPressStateFrom(eventData);
        }

        private void ProcessPointerButtonDrag(ref PointerModel.ButtonState button, ExtendedPointerEventData eventData)
        {
            if (!eventData.IsPointerMoving() ||
                (eventData.pointerType == UIPointerType.MouseOrPen && Cursor.lockState == CursorLockMode.Locked) ||
                eventData.pointerDrag == null)
                return;

            if (!eventData.dragging)
            {
                if (!eventData.useDragThreshold || (eventData.pressPosition - eventData.position).sqrMagnitude >=
                    (double)eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold * (eventData.pointerType == UIPointerType.Tracked
                                                                                               ? m_TrackedDeviceDragThresholdMultiplier
                                                                                               : 1))
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
                button.CopyPressStateFrom(eventData);
            }
        }

        private static void ProcessPointerScroll(ref PointerModel pointer, PointerEventData eventData)
        {
            var scrollDelta = pointer.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
            {
                eventData.scrollDelta = scrollDelta;
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(eventData.pointerEnter);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, eventData, ExecuteEvents.scrollHandler);
            }
        }

        internal void ProcessNavigation(ref NavigationModel navigationState)
        {
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

            // Process move.
            var movement = navigationState.move;
            if (!usedSelectionChange && (!Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f)))
            {
                var time = InputRuntime.s_Instance.unscaledGameTime;
                var moveVector = navigationState.move;

                var moveDirection = MoveDirection.None;
                if (moveVector.sqrMagnitude > 0)
                {
                    if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
                        moveDirection = moveVector.x > 0 ? MoveDirection.Right : MoveDirection.Left;
                    else
                        moveDirection = moveVector.y > 0 ? MoveDirection.Up : MoveDirection.Down;
                }

                ////REVIEW: is resetting move repeats when direction changes really useful behavior?
                if (moveDirection != m_NavigationState.lastMoveDirection)
                    m_NavigationState.consecutiveMoveCount = 0;

                if (moveDirection != MoveDirection.None)
                {
                    var allow = true;
                    if (m_NavigationState.consecutiveMoveCount != 0)
                    {
                        if (m_NavigationState.consecutiveMoveCount > 1)
                            allow = time > m_NavigationState.lastMoveTime + moveRepeatRate;
                        else
                            allow = time > m_NavigationState.lastMoveTime + moveRepeatDelay;
                    }

                    if (allow)
                    {
                        var eventData = m_NavigationState.eventData;
                        if (eventData == null)
                        {
                            eventData = new AxisEventData(eventSystem);
                            m_NavigationState.eventData = eventData;
                        }
                        eventData.Reset();

                        eventData.moveVector = moveVector;
                        eventData.moveDir = moveDirection;

                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.moveHandler);
                        usedSelectionChange = eventData.used;

                        m_NavigationState.consecutiveMoveCount = m_NavigationState.consecutiveMoveCount + 1;
                        m_NavigationState.lastMoveTime = time;
                        m_NavigationState.lastMoveDirection = moveDirection;
                    }
                }
                else
                    m_NavigationState.consecutiveMoveCount = 0;
            }
            else
            {
                m_NavigationState.consecutiveMoveCount = 0;
            }

            // Process submit and cancel events.
            if (!usedSelectionChange && eventSystem.currentSelectedGameObject != null)
            {
                var data = GetBaseEventData();
                if (m_NavigationState.cancelButton.wasPressedThisFrame)
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
                if (!data.used && m_NavigationState.submitButton.wasPressedThisFrame)
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);
            }

            m_NavigationState.OnFrameFinished();
        }

        [FormerlySerializedAs("m_RepeatDelay")]
        [Tooltip("The Initial delay (in seconds) between an initial move action and a repeated move action.")]
        [SerializeField]
        private float m_MoveRepeatDelay = 0.5f;

        [FormerlySerializedAs("m_RepeatRate")]
        [Tooltip("The speed (in seconds) that the move action repeats itself once repeating (max 1 per frame).")]
        [SerializeField]
        private float m_MoveRepeatRate = 0.1f;

        [Tooltip("Scales the Eventsystem.DragThreshold, for tracked devices, to make selection easier.")]
        // Hide this while we still have to figure out what to do with this.
        private float m_TrackedDeviceDragThresholdMultiplier = 2.0f;

        /// <summary>
        /// Delay in seconds between an initial move action and a repeated move action while <see cref="move"/> is actuated.
        /// </summary>
        /// <remarks>
        /// While <see cref="move"/> is being held down, the input module will first wait for <see cref="moveRepeatDelay"/> seconds
        /// after the first actuation of <see cref="move"/> and then trigger a move event every <see cref="moveRepeatRate"/> seconds.
        /// </remarks>
        /// <seealso cref="moveRepeatRate"/>
        /// <seealso cref="AxisEventData"/>
        /// <see cref="move"/>
        public float moveRepeatDelay
        {
            get => m_MoveRepeatDelay;
            set => m_MoveRepeatDelay = value;
        }

        /// <summary>
        /// Delay in seconds between repeated move actions while <see cref="move"/> is actuated.
        /// </summary>
        /// <remarks>
        /// While <see cref="move"/> is being held down, the input module will first wait for <see cref="moveRepeatDelay"/> seconds
        /// after the first actuation of <see cref="move"/> and then trigger a move event every <see cref="moveRepeatRate"/> seconds.
        ///
        /// Note that a maximum of one <see cref="AxisEventData"/> will be sent per frame. This means that even if multiple time
        /// increments of the repeat delay have passed since the last update, only one move repeat event will be generated.
        /// </remarks>
        /// <seealso cref="moveRepeatDelay"/>
        /// <seealso cref="AxisEventData"/>
        /// <see cref="move"/>
        public float moveRepeatRate
        {
            get => m_MoveRepeatRate;
            set => m_MoveRepeatRate = value;
        }

        [Obsolete("'repeatRate' has been obsoleted; use 'moveRepeatRate' instead. (UnityUpgradable) -> moveRepeatRate", false)]
        public float repeatRate
        {
            get => moveRepeatRate;
            set => moveRepeatRate = value;
        }

        [Obsolete("'repeatDelay' has been obsoleted; use 'moveRepeatDelay' instead. (UnityUpgradable) -> moveRepeatDelay", false)]
        public float repeatDelay
        {
            get => moveRepeatDelay;
            set => moveRepeatDelay = value;
        }

        /// <summary>
        /// Scales the drag threshold of <c>EventSystem</c> for tracked devices to make selection easier.
        /// </summary>
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
        /// <remarks>
        /// The values read from this action determine <see cref="PointerEventData.position"/> and <see cref="PointerEventData.delta"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/>, this forms the basis for pointer-type UI input.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("Point");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="leftClick"/>
        /// <seealso cref="rightClick"/>
        /// <seealso cref="middleClick"/>
        /// <seealso cref="scrollWheel"/>
        public InputActionReference point
        {
            get => m_PointAction;
            set => SwapAction(ref m_PointAction, value, m_ActionsHooked, m_OnPointDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector2</c> scroll wheel value
        /// used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        /// <remarks>
        /// The values read from this action determine <see cref="PointerEventData.scrollDelta"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="point"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that the action is optional. A pointer is fully functional with just <see cref="point"/>
        /// and <see cref="leftClick"/> alone.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var scrollAction = map.AddAction("scroll");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// scrollAction.AddBinding("&lt;Mouse&gt;/scroll");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).scrollWheel =
        ///     InputActionReference.Create(scrollAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="leftClick"/>
        /// <seealso cref="rightClick"/>
        /// <seealso cref="middleClick"/>
        /// <seealso cref="point"/>
        public InputActionReference scrollWheel
        {
            get => m_ScrollWheelAction;
            set => SwapAction(ref m_ScrollWheelAction, value, m_ActionsHooked, m_OnScrollWheelDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines
        /// whether the left button of a pointer is pressed.
        /// </summary>
        /// <remarks>
        /// Clicks on this button will use <see cref="PointerEventData.InputButton.Left"/> for <see cref="PointerEventData.button"/>.
        ///
        /// Together with <see cref="point"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that together with <see cref="point"/>, this action is necessary for a pointer to be functional. The other clicks
        /// and <see cref="scrollWheel"/> are optional, however.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var clickAction = map.AddAction("click");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// clickAction.AddBinding("&lt;Mouse&gt;/leftButton");
        /// clickAction.AddBinding("&lt;Touchscreen&gt;/touch*/press");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(clickAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="rightClick"/>
        /// <seealso cref="middleClick"/>
        /// <seealso cref="scrollWheel"/>
        /// <seealso cref="point"/>
        public InputActionReference leftClick
        {
            get => m_LeftClickAction;
            set => SwapAction(ref m_LeftClickAction, value, m_ActionsHooked, m_OnLeftClickDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines
        /// whether the middle button of a pointer is pressed.
        /// </summary>
        /// <remarks>
        /// Clicks on this button will use <see cref="PointerEventData.InputButton.Middle"/> for <see cref="PointerEventData.button"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="scrollWheel"/>, and
        /// <see cref="point"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that the action is optional. A pointer is fully functional with just <see cref="point"/>
        /// and <see cref="leftClick"/> alone.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var leftClickAction = map.AddAction("leftClick");
        /// var middleClickAction = map.AddAction("middleClick");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// leftClickAction.AddBinding("&lt;Mouse&gt;/leftButton");
        /// leftClickAction.AddBinding("&lt;Touchscreen&gt;/touch*/press");
        ///
        /// middleClickAction.AddBinding("&lt;Mouse&gt;/middleButton");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(leftClickAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).middleClick =
        ///     InputActionReference.Create(middleClickAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="leftClick"/>
        /// <seealso cref="rightClick"/>
        /// <seealso cref="scrollWheel"/>
        /// <seealso cref="point"/>
        public InputActionReference middleClick
        {
            get => m_MiddleClickAction;
            set => SwapAction(ref m_MiddleClickAction, value, m_ActionsHooked, m_OnMiddleClickDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float"</c> button value that determines
        /// whether the right button of a pointer is pressed.
        /// </summary>
        /// <remarks>
        /// Clicks on this button will use <see cref="PointerEventData.InputButton.Right"/> for <see cref="PointerEventData.button"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="middleClick"/>, <see cref="scrollWheel"/>, and
        /// <see cref="point"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that the action is optional. A pointer is fully functional with just <see cref="point"/>
        /// and <see cref="leftClick"/> alone.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var leftClickAction = map.AddAction("leftClick");
        /// var rightClickAction = map.AddAction("rightClick");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// leftClickAction.AddBinding("&lt;Mouse&gt;/leftButton");
        /// leftClickAction.AddBinding("&lt;Touchscreen&gt;/touch*/press");
        ///
        /// rightClickAction.AddBinding("&lt;Mouse&gt;/rightButton");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(leftClickAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).rightClick =
        ///     InputActionReference.Create(rightClickAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="leftClick"/>
        /// <seealso cref="middleClick"/>
        /// <seealso cref="scrollWheel"/>
        /// <seealso cref="point"/>
        public InputActionReference rightClick
        {
            get => m_RightClickAction;
            set => SwapAction(ref m_RightClickAction, value, m_ActionsHooked, m_OnRightClickDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector2</c> 2D motion vector
        /// used for sending <see cref="AxisEventData"/> navigation events.
        /// </summary>
        /// <remarks>
        /// The events generated from this input will be received by <see cref="IMoveHandler.OnMove"/>.
        ///
        /// This action together with <see cref="submit"/> and <see cref="cancel"/> form the sources for navigation-style
        /// UI input.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("move");
        /// var submitAction = map.AddAction("submit");
        /// var cancelAction = map.AddAction("cancel");
        ///
        /// moveAction.AddBinding("&lt;Gamepad&gt;/*stick");
        /// moveAction.AddBinding("&lt;Gamepad&gt;/dpad");
        /// submitAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// cancelAction.AddBinding("&lt;Gamepad&gt;/buttonEast");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).move =
        ///     InputActionReference.Create(moveAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).submit =
        ///     InputActionReference.Create(submitAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).cancelAction =
        ///     InputActionReference.Create(cancelAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="submit"/>
        /// <seealso cref="cancel"/>
        public InputActionReference move
        {
            get => m_MoveAction;
            set => SwapAction(ref m_MoveAction, value, m_ActionsHooked, m_OnMoveDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines when <c>ISubmitHandler</c>
        /// is triggered.
        /// </summary>
        /// <remarks>
        /// The events generated from this input will be received by <see cref="ISubmitHandler"/>.
        ///
        /// This action together with <see cref="move"/> and <see cref="cancel"/> form the sources for navigation-style
        /// UI input.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("move");
        /// var submitAction = map.AddAction("submit");
        /// var cancelAction = map.AddAction("cancel");
        ///
        /// moveAction.AddBinding("&lt;Gamepad&gt;/*stick");
        /// moveAction.AddBinding("&lt;Gamepad&gt;/dpad");
        /// submitAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// cancelAction.AddBinding("&lt;Gamepad&gt;/buttonEast");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).move =
        ///     InputActionReference.Create(moveAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).submit =
        ///     InputActionReference.Create(submitAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).cancelAction =
        ///     InputActionReference.Create(cancelAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="move"/>
        /// <seealso cref="cancel"/>
        public InputActionReference submit
        {
            get => m_SubmitAction;
            set => SwapAction(ref m_SubmitAction, value, m_ActionsHooked, m_OnSubmitDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines when <c>ICancelHandler</c>
        /// is triggered.
        /// </summary>
        /// <remarks>
        /// The events generated from this input will be received by <see cref="ICancelHandler"/>.
        ///
        /// This action together with <see cref="move"/> and <see cref="submit"/> form the sources for navigation-style
        /// UI input.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("move");
        /// var submitAction = map.AddAction("submit");
        /// var cancelAction = map.AddAction("cancel");
        ///
        /// moveAction.AddBinding("&lt;Gamepad&gt;/*stick");
        /// moveAction.AddBinding("&lt;Gamepad&gt;/dpad");
        /// submitAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// cancelAction.AddBinding("&lt;Gamepad&gt;/buttonEast");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).move =
        ///     InputActionReference.Create(moveAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).submit =
        ///     InputActionReference.Create(submitAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).cancelAction =
        ///     InputActionReference.Create(cancelAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="move"/>
        /// <seealso cref="submit"/>
        public InputActionReference cancel
        {
            get => m_CancelAction;
            set => SwapAction(ref m_CancelAction, value, m_ActionsHooked, m_OnCancelDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Quaternion</c> value reflecting the orientation of <see cref="TrackedDevice"/>s.
        /// In combination with <see cref="trackedDevicePosition"/>, this is used to determine the transform of tracked devices from which
        /// to raycast into the UI scene.
        /// </summary>
        /// <remarks>
        /// <see cref="trackedDeviceOrientation"/> and <see cref="trackedDevicePosition"/> together replace <see cref="point"/> for
        /// UI input from <see cref="TrackedDevice"/>. Other than that, UI input for tracked devices is no different from "normal"
        /// pointer-type input. This means that <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/> can all be used for tracked device input like for regular pointer input.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var positionAction = map.AddAction("position");
        /// var orientationAction = map.AddAction("orientation");
        /// var clickAction = map.AddAction("click");
        ///
        /// positionAction.AddBinding("&lt;TrackedDevice&gt;/devicePosition");
        /// orientationAction.AddBinding("&lt;TrackedDevice&gt;/deviceRotation");
        /// clickAction.AddBinding("&lt;TrackedDevice&gt;/trigger");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDevicePosition =
        ///     InputActionReference.Create(positionAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDeviceOrientation =
        ///     InputActionReference.Create(orientationAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(clickAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="trackedDevicePosition"/>
        public InputActionReference trackedDeviceOrientation
        {
            get => m_TrackedDeviceOrientationAction;
            set => SwapAction(ref m_TrackedDeviceOrientationAction, value, m_ActionsHooked, m_OnTrackedDeviceOrientationDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector3</c> value reflecting the position of <see cref="TrackedDevice"/>s.
        /// In combination with <see cref="trackedDeviceOrientation"/>, this is used to determine the transform of tracked devices from which
        /// to raycast into the UI scene.
        /// </summary>
        /// <remarks>
        /// <see cref="trackedDeviceOrientation"/> and <see cref="trackedDevicePosition"/> together replace <see cref="point"/> for
        /// UI input from <see cref="TrackedDevice"/>. Other than that, UI input for tracked devices is no different from "normal"
        /// pointer-type input. This means that <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/> can all be used for tracked device input like for regular pointer input.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var positionAction = map.AddAction("position");
        /// var orientationAction = map.AddAction("orientation");
        /// var clickAction = map.AddAction("click");
        ///
        /// positionAction.AddBinding("&lt;TrackedDevice&gt;/devicePosition");
        /// orientationAction.AddBinding("&lt;TrackedDevice&gt;/deviceRotation");
        /// clickAction.AddBinding("&lt;TrackedDevice&gt;/trigger");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDevicePosition =
        ///     InputActionReference.Create(positionAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDeviceOrientation =
        ///     InputActionReference.Create(orientationAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(clickAction);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="trackedDeviceOrientation"/>
        public InputActionReference trackedDevicePosition
        {
            get => m_TrackedDevicePositionAction;
            set => SwapAction(ref m_TrackedDevicePositionAction, value, m_ActionsHooked, m_OnTrackedDevicePositionDelegate);
        }

        [Obsolete("'trackedDeviceSelect' has been obsoleted; use 'leftClick' instead.", true)]
        public InputActionReference trackedDeviceSelect
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        protected override void Awake()
        {
            base.Awake();

            m_NavigationState.Reset();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnhookActions();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_OnControlsChangedDelegate == null)
                m_OnControlsChangedDelegate = OnControlsChanged;
            InputActionState.s_OnActionControlsChanged.AppendWithCapacity(m_OnControlsChangedDelegate);

            HookActions();
            EnableAllActions();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            var i = InputActionState.s_OnActionControlsChanged.IndexOfReference(m_OnControlsChangedDelegate);
            if (i != -1)
                InputActionState.s_OnActionControlsChanged.RemoveAtWithCapacity(i);

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
                (m_TrackedDevicePositionAction?.action?.enabled ?? true);
        }

        private void EnableAllActions()
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
                m_OwnsEnabledState = true;
            }
        }

        private void DisableAllActions()
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
            }
        }

        private int GetPointerStateIndexFor(int pointerId)
        {
            if (pointerId == m_CurrentPointerId)
                return m_CurrentPointerIndex;

            for (var i = 0; i < m_PointerIds.length; ++i)
                if (m_PointerIds[i] == pointerId)
                    return i;

            return -1;
        }

        private ref PointerModel GetPointerStateForIndex(int index)
        {
            if (index == 0)
                return ref m_PointerStates.firstValue;
            return ref m_PointerStates.additionalValues[index - 1];
        }

        private ref PointerModel GetPointerStateFor(ref InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(context.control);
            return ref GetPointerStateForIndex(index);
        }

        // This is the key method for determining which pointer a particular input is associated with.
        // The principal determinant is the device that is sending the input which, in general, is expected
        // to be a Pointer (Mouse, Pen, Touchscreen) or TrackedDevice.
        //
        // Note, however, that the input is not guaranteed to even come from a pointer-like device. One can
        // bind the space key to a left click, for example. As long as we have an active pointer that can
        // deliver position input, we accept that setup and treat pressing the space key the same as pressing
        // the left button input on the respective pointer.
        //
        // Quite a lot going on in this method but we're dealing with three different UI interaction paradigms
        // here which we all support from a single input path and allow seamless switching between.
        private int GetPointerStateIndexFor(InputControl control)
        {
            Debug.Assert(control != null, "Control must not be null");

            ////REVIEW: Any way we can cut down on the hops all over memory that we're doing here?
            var device = control.device;

            ////TODO: We're repeatedly inspecting the control setup here. Do this once and only redo it if the control setup changes.

            ////REVIEW: It seems wrong that we are picking up an input here that is *NOT* reflected in our actions. We just end
            ////        up reading a touchId control implicitly instead of allowing actions to deliver IDs to us. On the other hand,
            ////        making that setup explicit in actions may be quite awkward and not nearly as robust.
            // Determine the pointer (and touch) ID. We default the pointer ID to the device
            // ID of the InputDevice.
            var pointerId = device.deviceId;
            var touchId = 0;
            var controlParent = control.parent;
            if (controlParent is TouchControl touchControl)
                touchId = touchControl.touchId.ReadValue();
            // Could be it's a toplevel control on Touchscreen (like "<Touchscreen>/position"). In that case,
            // read the touch ID from primaryTouch.
            else if (controlParent is Touchscreen touchscreen)
                touchId = touchscreen.primaryTouch.touchId.ReadValue();
            if (touchId != 0)
                pointerId = ExtendedPointerEventData.MakePointerIdForTouch(pointerId, touchId);

            // Early out if it's the last used pointer.
            // NOTE: Can't just compare by device here because of touchscreens potentially having multiple associated pointers.
            if (m_CurrentPointerId == pointerId)
                return m_CurrentPointerIndex;

            // Search m_PointerIds for an existing entry.
            // NOTE: This is a linear search but m_PointerIds is only IDs and the number of concurrent pointers
            //       should be very low at any one point (in fact, we don't generally expect to have more than one
            //       which is why we are using InlinedArrays).
            for (var i = 0; i < m_PointerIds.length; i++)
            {
                if (m_PointerIds[i] == pointerId)
                {
                    // Existing entry found. Make it the current pointer.
                    m_CurrentPointerId = pointerId;
                    m_CurrentPointerIndex = i;
                    m_CurrentPointerType = m_PointerStates[i].pointerType;
                    return i;
                }
            }

            // Determine pointer type.
            var pointerType = UIPointerType.None;
            if (touchId != 0)
                pointerType = UIPointerType.Touch;
            else if (HaveControlForDevice(device, point))
                pointerType = UIPointerType.MouseOrPen;
            else if (HaveControlForDevice(device, trackedDevicePosition))
                pointerType = UIPointerType.Tracked;

            // For SingleMouseOrPenButMultiTouchAndTrack, we keep a single pointer for mouse and pen but only for as
            // long as there is no touch or tracked input. If we get that kind, we remove the mouse/pen pointer.
            if (m_PointerBehavior == UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack && pointerType != UIPointerType.None)
            {
                if (pointerType == UIPointerType.MouseOrPen)
                {
                    // We have input on a mouse or pen. Kill all touch and tracked pointers we may have.
                    for (var i = 0; i < m_PointerStates.length; ++i)
                    {
                        if (m_PointerStates[i].pointerType != UIPointerType.MouseOrPen)
                        {
                            SendPointerExitEventsAndRemovePointer(i);
                            --i;
                        }
                    }
                }
                else
                {
                    // We have touch or tracked input. Kill mouse/pen pointer, if we have it.
                    for (var i = 0; i < m_PointerStates.length; ++i)
                    {
                        if (m_PointerStates[i].pointerType == UIPointerType.MouseOrPen)
                        {
                            SendPointerExitEventsAndRemovePointer(i);
                            --i;
                        }
                    }
                }
            }
            ////REVIEW: For touch, probably makes sense to force-ignore any input other than from primaryTouch.
            // If the behavior is SingleUnifiedPointer, we only ever create a single pointer state
            // and use that for all pointer input that is coming in.
            if ((m_PointerBehavior == UIPointerBehavior.SingleUnifiedPointer && pointerType != UIPointerType.None) ||
                (m_PointerBehavior == UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack && pointerType == UIPointerType.MouseOrPen))
            {
                if (m_CurrentPointerIndex == -1)
                {
                    m_CurrentPointerIndex = AllocatePointer(pointerId, touchId, pointerType, device);
                }
                else
                {
                    // Update pointer record to reflect current device. We know they're different because we checked
                    // m_CurrentPointerId earlier in the method.
                    // NOTE: This path may repeatedly switch the pointer type and ID on the same single event instance.

                    ref var pointer = ref GetPointerStateForIndex(m_CurrentPointerIndex);

                    var eventData = pointer.eventData;
                    eventData.device = device;
                    eventData.pointerType = pointerType;
                    eventData.pointerId = pointerId;
                    eventData.touchId = touchId;

                    // Make sure these don't linger around when we switch to a different kind of pointer.
                    eventData.trackedDeviceOrientation = default;
                    eventData.trackedDevicePosition = default;
                }

                m_CurrentPointerId = pointerId;
                m_CurrentPointerType = pointerType;

                return m_CurrentPointerIndex;
            }

            // No existing record for the device. Find out if the device has the ability to point at all.
            // If not, we need to use a pointer state from a different device (if present).
            var index = -1;
            if (pointerType != UIPointerType.None)
            {
                // Device has an associated position input. Create a new pointer record.
                index = AllocatePointer(pointerId, touchId, pointerType, device);
            }
            else
            {
                // Device has no associated position input. Find a pointer device to route the change into.
                // As a last resort, create a pointer without a position input.

                // If we have a current pointer, route the input into that. The majority of times we end
                // up in this branch, this should settle things.
                if (m_CurrentPointerId != -1)
                    return m_CurrentPointerIndex;

                // NOTE: In most cases, we end up here when there is input on a non-pointer device bound to one of the pointer-related
                //       actions before there is input from a pointer device. In this scenario, we don't have a pointer state allocated
                //       for the device yet.

                // If we have anything bound to the `point` action, create a pointer for it.
                var pointControls = point?.action?.controls;
                var pointerDevice = pointControls.HasValue && pointControls.Value.Count > 0 ? pointControls.Value[0].device : null;
                if (pointerDevice != null && !(pointerDevice is Touchscreen)) // Touchscreen only temporarily allocate pointer states.
                {
                    // Create MouseOrPen style pointer.
                    index = AllocatePointer(pointerDevice.deviceId, 0, UIPointerType.MouseOrPen, pointerDevice);
                }
                else
                {
                    // Do the same but look at the `position` action.
                    var positionControls = trackedDevicePosition?.action?.controls;
                    var trackedDevice = positionControls.HasValue && positionControls.Value.Count > 0
                        ? positionControls.Value[0].device
                        : null;
                    if (trackedDevice != null)
                    {
                        // Create a Tracked style pointer.
                        index = AllocatePointer(trackedDevice.deviceId, 0, UIPointerType.Tracked, trackedDevice);
                    }
                    else
                    {
                        // We got input from a non-pointer device and apparently there's no pointer we can route the
                        // input into. Just create a pointer state for the device and leave it at that.
                        index = AllocatePointer(pointerId, 0, UIPointerType.None, device);
                    }
                }
            }

            m_CurrentPointerId = pointerId;
            m_CurrentPointerIndex = index;
            m_CurrentPointerType = pointerType;

            return index;
        }

        private int AllocatePointer(int pointerId, int touchId, UIPointerType pointerType, InputDevice device)
        {
            // Recover event instance from previous record.
            var eventData = default(ExtendedPointerEventData);
            if (m_PointerStates.Capacity > m_PointerStates.length)
            {
                if (m_PointerStates.length == 0)
                    eventData = m_PointerStates.firstValue.eventData;
                else
                    eventData = m_PointerStates.additionalValues[m_PointerStates.length - 1].eventData;
            }

            // Or allocate event.
            if (eventData == null)
                eventData = new ExtendedPointerEventData(eventSystem);

            // Allocate state.
            m_PointerIds.AppendWithCapacity(pointerId);
            return m_PointerStates.AppendWithCapacity(new PointerModel(pointerId, touchId, pointerType, device, eventData));
        }

        private void SendPointerExitEventsAndRemovePointer(int index)
        {
            var eventData = m_PointerStates[index].eventData;
            if (eventData.pointerEnter != null)
                ProcessPointerMovement(eventData, null);

            RemovePointerAtIndex(index);
        }

        private void RemovePointerAtIndex(int index)
        {
            Debug.Assert(m_PointerStates[index].eventData.pointerEnter == null, "Pointer should have exited all objects before being removed");

            // Retain event data so that we can reuse the event the next time we allocate a PointerModel record.
            var eventData = m_PointerStates[index].eventData;
            Debug.Assert(eventData != null, "Pointer state should have an event instance!");

            // Remove. Note that we may change the order of pointers here. This can save us needless copying
            // and m_CurrentPointerIndex should be the only index we get around for longer.
            m_PointerIds.RemoveAtByMovingTailWithCapacity(index);
            m_PointerStates.RemoveAtByMovingTailWithCapacity(index);
            Debug.Assert(m_PointerIds.length == m_PointerStates.length, "Pointer ID array should match state array in length");

            if (index == m_CurrentPointerIndex)
            {
                m_CurrentPointerId = -1;
                m_CurrentPointerIndex = -1;
            }

            // Put event instance back in place at one past last entry of array (which we know we have
            // as we just erased one entry). This entry will be the next one that will be used when we
            // allocate a new entry.

            // Wipe the event.
            // NOTE: We only wipe properties here that contain reference data. The rest we rely on
            //       the event handling code to initialize when using the event.
            eventData.hovered.Clear();
            eventData.device = null;
            eventData.pointerCurrentRaycast = default;
            eventData.pointerPressRaycast = default;
            eventData.pointerPress = default; // Twice to wipe lastPress, too.
            eventData.pointerPress = default;
            eventData.pointerDrag = default;
            eventData.pointerEnter = default;
            eventData.rawPointerPress = default;

            if (m_PointerStates.length == 0)
                m_PointerStates.firstValue.eventData = eventData;
            else
                m_PointerStates.additionalValues[m_PointerStates.length - 1].eventData = eventData;
        }

        // Remove any pointer that no longer has the ability to point.
        private void PurgeStalePointers()
        {
            for (var i = 0; i < m_PointerStates.length; ++i)
            {
                ref var state = ref GetPointerStateForIndex(i);
                var device = state.eventData.device;
                if (!HaveControlForDevice(device, point) &&
                    !HaveControlForDevice(device, trackedDevicePosition) &&
                    !HaveControlForDevice(device, trackedDeviceOrientation))
                {
                    SendPointerExitEventsAndRemovePointer(i);
                    --i;
                }
            }
        }

        private static bool HaveControlForDevice(InputDevice device, InputActionReference actionReference)
        {
            var action = actionReference?.action;
            if (action == null)
                return false;

            var controls = action.controls;
            for (var i = 0; i < controls.Count; ++i)
                if (controls[i].device == device)
                    return true;

            return false;
        }

        private void OnPoint(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            state.screenPosition = context.ReadValue<Vector2>();
        }

        ////REVIEW: How should we handle clickCount here? There's only one for the entire device yet right and middle clicks
        ////        are independent of left clicks. ATM we ignore native click counts and do click detection for all clicks
        ////        ourselves just like StandaloneInputModule does.

        private void OnLeftClick(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            state.leftButton.isPressed = context.ReadValueAsButton();
            state.changedThisFrame = true;
        }

        private void OnRightClick(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            state.rightButton.isPressed = context.ReadValueAsButton();
            state.changedThisFrame = true;
        }

        private void OnMiddleClick(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            state.middleButton.isPressed = context.ReadValueAsButton();
            state.changedThisFrame = true;
        }

        internal const float kPixelPerLine = 20;

        private void OnScroll(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            // The old input system reported scroll deltas in lines, we report pixels.
            // Need to scale as the UI system expects lines.
            state.scrollDelta = context.ReadValue<Vector2>() * (1 / kPixelPerLine);
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            m_NavigationState.move = context.ReadValue<Vector2>();
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            m_NavigationState.submitButton.isPressed = context.ReadValueAsButton();
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            m_NavigationState.cancelButton.isPressed = context.ReadValueAsButton();
        }

        private void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            state.worldOrientation = context.ReadValue<Quaternion>();
        }

        private void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
            ref var state = ref GetPointerStateFor(ref context);
            state.worldPosition = context.ReadValue<Vector3>();
        }

        private void OnControlsChanged(object obj)
        {
            PurgeStalePointers();
        }

        public override void Process()
        {
            // Reset devices of changes since we don't want to spool up changes once we gain focus.
            if (!eventSystem.isFocused)
            {
                m_NavigationState.OnFrameFinished();
                for (var i = 0; i < m_PointerStates.length; ++i)
                    m_PointerStates[i].OnFrameFinished();
            }
            else
            {
                ProcessNavigation(ref m_NavigationState);
                for (var i = 0; i < m_PointerStates.length; i++)
                {
                    ref var state = ref GetPointerStateForIndex(i);
                    ProcessPointer(ref state);

                    // If it's a touch and the touch has ended, release the pointer state.
                    // NOTE: We have no guarantee that the system reuses touch IDs so the touch ID we used
                    //       as a pointer ID may be a one-off thing.
                    if (state.pointerType == UIPointerType.Touch && !state.leftButton.isPressed)
                    {
                        RemovePointerAtIndex(i);
                        --i;
                    }
                }
            }
        }

        private void HookActions()
        {
            if (m_ActionsHooked)
                return;

            if (m_OnPointDelegate == null)
                m_OnPointDelegate = OnPoint;
            if (m_OnLeftClickDelegate == null)
                m_OnLeftClickDelegate = OnLeftClick;
            if (m_OnRightClickDelegate == null)
                m_OnRightClickDelegate = OnRightClick;
            if (m_OnMiddleClickDelegate == null)
                m_OnMiddleClickDelegate = OnMiddleClick;
            if (m_OnScrollWheelDelegate == null)
                m_OnScrollWheelDelegate = OnScroll;
            if (m_OnMoveDelegate == null)
                m_OnMoveDelegate = OnMove;
            if (m_OnSubmitDelegate == null)
                m_OnSubmitDelegate = OnSubmit;
            if (m_OnCancelDelegate == null)
                m_OnCancelDelegate = OnCancel;
            if (m_OnTrackedDeviceOrientationDelegate == null)
                m_OnTrackedDeviceOrientationDelegate = OnTrackedDeviceOrientation;
            if (m_OnTrackedDevicePositionDelegate == null)
                m_OnTrackedDevicePositionDelegate = OnTrackedDevicePosition;

            SetActionCallbacks(true);
        }

        private void UnhookActions()
        {
            if (!m_ActionsHooked)
                return;

            SetActionCallbacks(false);
        }

        private void SetActionCallbacks(bool install)
        {
            m_ActionsHooked = install;
            SetActionCallback(m_PointAction, m_OnPointDelegate, install);
            SetActionCallback(m_MoveAction, m_OnMoveDelegate, install);
            SetActionCallback(m_LeftClickAction, m_OnLeftClickDelegate, install);
            SetActionCallback(m_RightClickAction, m_OnRightClickDelegate, install);
            SetActionCallback(m_MiddleClickAction, m_OnMiddleClickDelegate, install);
            SetActionCallback(m_SubmitAction, m_OnSubmitDelegate, install);
            SetActionCallback(m_CancelAction, m_OnCancelDelegate, install);
            SetActionCallback(m_ScrollWheelAction, m_OnScrollWheelDelegate, install);
            SetActionCallback(m_TrackedDeviceOrientationAction, m_OnTrackedDeviceOrientationDelegate, install);
            SetActionCallback(m_TrackedDevicePositionAction, m_OnTrackedDevicePositionDelegate, install);
        }

        private static void SetActionCallback(InputActionReference actionReference, Action<InputAction.CallbackContext> callback, bool install)
        {
            if (!install && callback == null)
                return;

            if (actionReference == null)
                return;

            var action = actionReference.action;
            if (action == null)
                return;

            if (install)
            {
                action.performed += callback;
                action.canceled += callback;
            }
            else
            {
                action.performed -= callback;
                action.canceled -= callback;
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

        [SerializeField, HideInInspector] private InputActionAsset m_ActionsAsset;
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

        [SerializeField] private bool m_DeselectOnBackgroundClick = true;
        [SerializeField] private UIPointerBehavior m_PointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;

        private bool m_OwnsEnabledState;
        private bool m_ActionsHooked;

        private Action<InputAction.CallbackContext> m_OnPointDelegate;
        private Action<InputAction.CallbackContext> m_OnMoveDelegate;
        private Action<InputAction.CallbackContext> m_OnSubmitDelegate;
        private Action<InputAction.CallbackContext> m_OnCancelDelegate;
        private Action<InputAction.CallbackContext> m_OnLeftClickDelegate;
        private Action<InputAction.CallbackContext> m_OnRightClickDelegate;
        private Action<InputAction.CallbackContext> m_OnMiddleClickDelegate;
        private Action<InputAction.CallbackContext> m_OnScrollWheelDelegate;
        private Action<InputAction.CallbackContext> m_OnTrackedDevicePositionDelegate;
        private Action<InputAction.CallbackContext> m_OnTrackedDeviceOrientationDelegate;
        private Action<object> m_OnControlsChangedDelegate;

        // Pointer-type input (also tracking-type).
        private int m_CurrentPointerId = -1; // Keeping track of the current pointer avoids searches in most cases.
        private int m_CurrentPointerIndex = -1;
        private UIPointerType m_CurrentPointerType = UIPointerType.None;
        private InlinedArray<int> m_PointerIds; // Index in this array maps to index in m_PointerStates. Separated out to make searching more efficient (we do a linear search).
        private InlinedArray<PointerModel> m_PointerStates;

        // Navigation-type input.
        private NavigationModel m_NavigationState;
    }
}
