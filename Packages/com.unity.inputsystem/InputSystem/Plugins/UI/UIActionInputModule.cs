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

        protected override void OnDestroy()
        {
            UnhookActions();
        }

        protected override void Awake()
        {
            base.Awake();

            //TODO TB figure out why the pointer Ids change for every click and mouse press. Use 0 for now.
            mouseState = new MockMouseState(eventSystem, 0);
            joystickState.Reset();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            HookActions();
            EnableActions();
        }

        protected override void OnDisable()
        {
            UnhookActions();
            DisableActions();
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

        PointerEventData PrepareInitialEventData(MockMouseState mouseState)
        {
            var eventData = GetOrCreateCachedPointerEvent();
            eventData.pointerId = mouseState.pointerId;
            eventData.position = mouseState.position;
            eventData.delta = mouseState.deltaPosition;
            eventData.scrollDelta = mouseState.scrollDelta;
            eventData.pointerEnter = mouseState.pointerTarget;

            // Defaults (Unset)
            eventData.useDragThreshold = true;

            eventData.pointerCurrentRaycast = PerformRaycast(eventData);
            return eventData;
        }

        AxisEventData PrepareAxisEventData(MockJoystick joystickState)
        {
            //TODO TB (Don't be so wasteful)
            AxisEventData eventData = new AxisEventData(eventSystem);

            eventData.Reset();
            Vector2 moveVector = eventData.moveVector = joystickState.move;

            if (moveVector.sqrMagnitude < moveDeadzone * moveDeadzone)
                eventData.moveDir = MoveDirection.None;
            else if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
                eventData.moveDir = (moveVector.x > 0) ? MoveDirection.Right : MoveDirection.Left;
            else
                eventData.moveDir = (moveVector.y > 0) ? MoveDirection.Up : MoveDirection.Down;

            return eventData;
        }

        // walk up the tree till a common root between the last entered and the current entered is foung
        // send exit events up to (but not inluding) the common root. Then send enter events up to
        // (but not including the common root).
        void HandleEnterAndExit(PointerEventData eventData)
        {
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

        void HandleMouseClick(ButtonDeltaState mouseButtonChanges, PointerEventData eventData)
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

        void HandleMouseDrag(PointerEventData eventData)
        {
            if (!eventData.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                eventData.pointerDrag == null)
                return;

            if(!eventData.dragging)
            {
                if((eventData.pressPosition - eventData.position).sqrMagnitude >= (eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold))
                {
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                    eventData.dragging = true;
                }
            }

            if(eventData.dragging)
            {
                // If we moved from our initial press object
                if(eventData.pointerPress != eventData.pointerDrag)
                {
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                    eventData.eligibleForClick = false;
                    eventData.pointerPress = null;
                    eventData.rawPointerPress = null;
                }
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
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
            else if(action == m_CancelAction)
            {
                joystickState.cancelButtonDown = context.ReadValue<float>() != 0.0f;
            }
        }

        void ProcessMouse()
        {
            if (!mouseState.changedThisFrame)
                return;

            var eventData = PrepareInitialEventData(mouseState);

            /// Left Mouse Button
            // The left mouse button is 'dominant' and we want to also process hover and scroll events as if the occurred during the left click.
            var buttonState = mouseState.leftButton;
            buttonState.CopyTo(eventData);
            HandleMouseClick(buttonState.lastFrameDelta, eventData);

            HandleEnterAndExit(eventData);
            mouseState.hoverTargets = eventData.hovered;
            mouseState.pointerTarget = eventData.pointerEnter;

            var scrollDelta = mouseState.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(mouseState.pointerTarget);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, eventData, ExecuteEvents.scrollHandler);
            }

            HandleMouseDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.leftButton = buttonState;

            /// Right Mouse Button
            buttonState = mouseState.rightButton;
            buttonState.CopyTo(eventData);

            HandleMouseClick(buttonState.lastFrameDelta, eventData);
            HandleMouseDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.rightButton = buttonState;

            /// Middle Mouse Button
            buttonState = mouseState.middleButton;
            buttonState.CopyTo(eventData);

            HandleMouseClick(buttonState.lastFrameDelta, eventData);
            HandleMouseDrag(eventData);

            buttonState.CopyFrom(eventData);
            mouseState.middleButton = buttonState;

            mouseState.OnFrameFinished();
        }

        void ProcessJoystick()
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

            Vector2 movement = joystickState.move;
            if (!usedSelectionChange && (!Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f)))
            {
                float time = Time.unscaledTime;

                Vector2 moveVector = joystickState.move;

                MoveDirection moveDirection = MoveDirection.None;
                if(moveVector.sqrMagnitude > moveDeadzone * moveDeadzone)
                {
                    if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
                        moveDirection = (moveVector.x > 0) ? MoveDirection.Right : MoveDirection.Left;
                    else
                        moveDirection = (moveVector.y > 0) ? MoveDirection.Up : MoveDirection.Down;
                }

                if (moveDirection != joystickState.lastMoveDirection)
                {
                    joystickState.consecutiveMoveCount = 0;
                }

                if (moveDirection != MoveDirection.None)
                {
                    bool allow = true;
                    if (joystickState.consecutiveMoveCount != 0)
                    {
                        if (joystickState.consecutiveMoveCount > 1)
                            allow = (time > (joystickState.lastMoveTime + repeatRate));
                        else
                            allow = (time > (joystickState.lastMoveTime + repeatDelay));
                    }

                    if (allow)
                    {
                        //TODO TB (Don't be so wasteful)
                        AxisEventData eventData = new AxisEventData(eventSystem);
                        eventData.Reset();

                        eventData.moveVector = moveVector;
                        eventData.moveDir = moveDirection;

                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.moveHandler);
                        usedSelectionChange = eventData.used;

                        joystickState.consecutiveMoveCount = joystickState.consecutiveMoveCount + 1;
                        joystickState.lastMoveTime = time;
                        joystickState.lastMoveDirection = moveDirection;
                        
                    }
                }
                else
                    joystickState.consecutiveMoveCount = 0;
            } 
            else
                joystickState.consecutiveMoveCount = 0;

            if(!usedSelectionChange)
            {
                if(eventSystem.currentSelectedGameObject != null)
                {
                    var data = GetBaseEventData();
                    if((joystickState.submitButtonDelta & ButtonDeltaState.Pressed) != 0)
                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

                    if(!data.used && (joystickState.cancelButtonDelta & ButtonDeltaState.Released) != 0)
                        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
                }
            }

            joystickState.OnFrameFinished();
        }

        public override void Process()
        {
            if(!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                joystickState.OnFrameFinished();
                mouseState.OnFrameFinished();
            }

            ProcessJoystick();
            ProcessMouse();
        }

        private void EnableActions()
        {
            if(!m_ActionsEnabled)
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

                m_ActionsEnabled = true;
            }           
        }

        private void DisableActions()
        {
            if(m_ActionsEnabled)
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

                m_ActionsEnabled = false;
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
                if (m_ActionsEnabled)
                    oldProperty.action.Disable();

                if (m_ActionsHooked)
                    oldProperty.action.performed -= m_ActionCallback;
            }

            oldProperty = newProperty;

            if (oldProperty != null)
            {
                if (m_ActionsEnabled)
                    oldProperty.action.Enable();

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

        [Tooltip("The maximum time (in seconds) between two mouse presses for it to be consecutive click.")]
        public float clickSpeed = 0.3f;

        [Tooltip("The absolute value required by a move action on either axis required to trigger a move event.")]
        public float moveDeadzone = 0.6f;

        [Tooltip("The Initial delay (in seconds) between an initial move action and a repeated move action.")]
        public float repeatDelay = 0.5f;

        [Tooltip("The speed (in seconds) that the move action repeats itself once repeating.")]
        public float repeatRate = 0.1f;

        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private bool m_ActionsEnabled;
        [NonSerialized] private Action<InputAction.CallbackContext> m_ActionCallback;

        [NonSerialized] private int m_LastPointerId;
        [NonSerialized] private Vector2 m_LastPointerPosition;

        [NonSerialized] private MockMouseState mouseState;
        [NonSerialized] private MockJoystick joystickState;
    }
}
