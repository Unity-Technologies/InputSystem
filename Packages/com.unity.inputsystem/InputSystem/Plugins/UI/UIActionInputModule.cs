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
            mockMouseStates = new List<MockMouseState>();
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

        int GetPointerIdFromControl(InputControl control)
        {
            //TODO TB figure out why the pointer Ids change for every click and mouse press
            /*
            var device = control != null ? control.device : null;
            var pointer = device as Pointer;
            return pointer != null ? pointer.pointerId.ReadValue() : 0;
            */
            return 0;
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

            GameObject commonRoot = FindCommonRoot(eventData.pointerEnter, currentPointerTarget);

            // and we already an entered object from last time
            if (eventData.pointerEnter != null)
            {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                Transform t = eventData.pointerEnter.transform;

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
                Transform t = currentPointerTarget.transform;

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

        private delegate void ModifyMouseStateDel(ref MockMouseState mouseState);
        void ModifyMouseState(int pointerId, ModifyMouseStateDel func)
        {
            int i = 0;
            for (; i < mockMouseStates.Count; i++)
            {
                if (mockMouseStates[i].pointerId == pointerId)
                {
                    MockMouseState mouseState = mockMouseStates[i];
                    func(ref mouseState);

                    mockMouseStates[i] = mouseState;
                    return;
                }
            }

            // No mouse state with that Id
            {
                MockMouseState mouseState = new MockMouseState(eventSystem, pointerId);
                func(ref mouseState);
                mockMouseStates.Add(mouseState);
            }
        }

        public void OnAction(InputAction.CallbackContext context)
        {
            var action = context.action;
            var pointerId = GetPointerIdFromControl(context.control);
            if (action == m_PointAction)
            {
                ModifyMouseState(pointerId, (ref MockMouseState state) =>
                {
                    Vector2 position = context.ReadValue<Vector2>();
                    state.position = position;
                });
            }
            else if (action == m_ScrollWheelAction)
            {
                ModifyMouseState(pointerId, (ref MockMouseState state) =>
                {
                    Vector2 scrollPosition = context.ReadValue<Vector2>();
                    state.scrollPosition = scrollPosition;
                });
            }
            else if (action == m_LeftClickAction)
            {
                ModifyMouseState(pointerId, (ref MockMouseState state) =>
                {
                    MockMouseButton buttonState = state.leftButton;
                    buttonState.isDown = context.ReadValue<float>() != 0.0f;
                    state.leftButton = buttonState;
                });
            }
            else if (action == m_RightClickAction)
            {
                ModifyMouseState(pointerId, (ref MockMouseState state) =>
                {
                    MockMouseButton buttonState = state.rightButton;
                    buttonState.isDown = context.ReadValue<float>() != 0.0f;
                    state.rightButton = buttonState;
                });
            }
            else if (action == m_MiddleClickAction)
            {
                ModifyMouseState(pointerId, (ref MockMouseState state) =>
                {
                    MockMouseButton buttonState = state.middleButton;
                    buttonState.isDown = context.ReadValue<float>() != 0.0f;
                    state.middleButton = buttonState;
                });
            }
            else if (action == m_MoveAction)
            {
                // Don't send move events if disabled in the EventSystem.
                if (!eventSystem.sendNavigationEvents)
                    return;
            }
        }

        public override void Process()
        {
            //Mouse needs Position, Delta Position, ScrollDelta, ButtonStatesMock (Left, Right,Middle)

            for(int i = 0; i < mockMouseStates.Count; i++)
            {              
                if(mockMouseStates[i].changedThisFrame)
                {
                    MockMouseState state = mockMouseStates[i];
                    bool mouseMoving = state.deltaPosition.sqrMagnitude > float.Epsilon;

                    PointerEventData eventData = PrepareInitialEventData(state);

                    // The left mouse button is 'dominant' and we want to also process hover and scroll events as if the occurred during the left click.
                    {
                        MockMouseButton buttonState = state.leftButton;
                        buttonState.CopyTo(eventData);
                        HandleMouseClick(buttonState.lastFrameDelta, eventData);

                        HandleEnterAndExit(eventData);
                        state.hoverTargets = eventData.hovered;
                        state.pointerTarget = eventData.pointerEnter;

                        Vector2 scrollDelta = state.scrollDelta;
                        if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
                        {
                            var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(state.pointerTarget);
                            ExecuteEvents.ExecuteHierarchy(scrollHandler, eventData, ExecuteEvents.scrollHandler);
                        }

                        HandleMouseDrag(eventData);

                        buttonState.CopyFrom(eventData);
                        state.leftButton = buttonState;
                    }

                    {
                        MockMouseButton buttonState = state.rightButton;
                        buttonState.CopyTo(eventData);

                        HandleMouseClick(buttonState.lastFrameDelta, eventData);
                        HandleMouseDrag(eventData);

                        buttonState.CopyFrom(eventData);
                        state.rightButton = buttonState;

                    }

                    {
                        MockMouseButton buttonState = state.middleButton;
                        buttonState.CopyTo(eventData);

                        HandleMouseClick(buttonState.lastFrameDelta, eventData);
                        HandleMouseDrag(eventData);

                        buttonState.CopyFrom(eventData);
                        state.middleButton = buttonState;

                    }

                    state.OnFrameFinished();
                    mockMouseStates[i] = state;
                }
            }
        }

        private void EnableActions()
        {
            if(!m_ActionsEnabled)
            {
                var pointAction = m_PointAction.action;
                if (pointAction != null && !pointAction.enabled)
                    pointAction.Enable();

                var moveAction = m_MoveAction.action;
                if (moveAction != null && !moveAction.enabled)
                    moveAction.Enable();

                var leftClickAction = m_LeftClickAction.action;
                if (leftClickAction != null && !leftClickAction.enabled)
                    leftClickAction.Enable();

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

                var moveAction = m_MoveAction.action;
                if (moveAction != null && moveAction.enabled)
                    moveAction.Disable();

                var leftClickAction = m_LeftClickAction.action;
                if (leftClickAction != null && leftClickAction.enabled)
                    leftClickAction.Disable();

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

        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private bool m_ActionsEnabled;
        [NonSerialized] private Action<InputAction.CallbackContext> m_ActionCallback;

        [NonSerialized] private int m_LastPointerId;
        [NonSerialized] private Vector2 m_LastPointerPosition;

        /// <summary>
        /// Queue where we record action events.
        /// </summary>
        /// <remarks>
        /// The callback-based interface isn't of much use to us as we cannot respond to actions immediately
        /// but have to wait until <see cref="Process"/> is called by <see cref="EventSystem"/>. So instead
        /// we trace everything that happens to the actions we're linked to by recording events into this queue
        /// and then during <see cref="Process"/> we replay any activity that has occurred since the last
        /// call to <see cref="Process"/> and translate it into <see cref="BaseEventData">UI events</see>.
        /// </remarks>
        [NonSerialized] private InputActionQueue m_ActionQueue;

        private List<MockMouseState> mockMouseStates;
    }
}
