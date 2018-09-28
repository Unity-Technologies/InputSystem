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

            if (m_ActionQueue != null)
            {
                m_ActionQueue.Dispose();
                m_ActionQueue = null;
            }
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
            var device = control != null ? control.device : null;
            var pointer = device as Pointer;
            return pointer != null ? pointer.pointerId.ReadValue() : 0;
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

        public override void Process()
        {
            //Mouse needs Position, Delta Position, ScrollDelta, ButtonStatesMock (Left, Right,Middle)

            foreach (var entry in m_ActionQueue)
            {
                var action = entry.action;
                Debug.Assert(action != null);
                if (action == null)
                    continue;

                if (action == m_PointAction)
                {
                    var pointerId = GetPointerIdFromControl(entry.control);
                    ModifyMouseState(pointerId, (ref MockMouseState state) => { state.position = entry.ReadValue<Vector2>(); });
                }
                else if(action == m_ScrollWheelAction)
                {
                    var pointerId = GetPointerIdFromControl(entry.control);
                    ModifyMouseState(pointerId, (ref MockMouseState state) => { state.scrollDelta = entry.ReadValue<Vector2>(); });
                }
                else if (action == m_LeftClickAction)
                {
                    var pointerId = GetPointerIdFromControl(entry.control);
                    ModifyMouseState(pointerId, (ref MockMouseState state) => { state.leftButton = entry.ReadValue<bool>(); });
                }
                else if (action == m_RightClickAction)
                {
                    var pointerId = GetPointerIdFromControl(entry.control);
                    ModifyMouseState(pointerId, (ref MockMouseState state) => { state.rightButton = entry.ReadValue<bool>(); });
                }
                else if (action == m_MiddleClickAction)
                {
                    var pointerId = GetPointerIdFromControl(entry.control);
                    ModifyMouseState(pointerId, (ref MockMouseState state) => { state.middleButton = entry.ReadValue<bool>(); });
                }
                else if (action == m_MoveAction)
                {
                    // Don't send move events if disabled in the EventSystem.
                    if (!eventSystem.sendNavigationEvents)
                        continue;
                }
            }

            m_ActionQueue.Clear();

            for(int i = 0; i < mockMouseStates.Count; i++)
            {              
                if(mockMouseStates[i].dirty)
                {
                    MockMouseState state = mockMouseStates[i];

                    var eventData = GetOrCreateCachedPointerEvent();
                    eventData.pointerId = state.pointerId;
                    eventData.position = state.position;
                    eventData.delta = state.deltaPosition;
                    eventData.scrollDelta = state.scrollDelta;

                    eventData.pointerCurrentRaycast = PerformRaycast(eventData);

                    //Handle Left Click

                    //Handle Move
                    HandlePointerExitAndEnter(eventData, eventData.pointerCurrentRaycast.gameObject);

                    //Handle Left Drag

                    //Handle Right Click

                    //Handle Right Drag

                    //Handle Middle Click

                    //Handle Middle Drag


                    mockMouseStates[i].ClearDirty();
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

            if (m_ActionQueue == null)
                m_ActionQueue = new InputActionQueue();
            if (m_ActionCallback == null)
                m_ActionCallback = m_ActionQueue.RecordAction;

            m_ActionsHooked = true;

            var pointAction = m_PointAction.action;
            if (pointAction != null)
                pointAction.performed += m_ActionCallback;

            var moveAction = m_MoveAction.action;
            if (moveAction != null)
                moveAction.performed += m_ActionCallback;

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
