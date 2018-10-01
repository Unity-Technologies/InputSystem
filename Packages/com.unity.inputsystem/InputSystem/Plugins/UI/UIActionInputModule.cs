using System;
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
            set
            {
                if (m_PointAction != null && m_ActionsHooked)
                    m_PointAction.action.performed -= m_ActionCallback;
                m_PointAction = value;
                if (m_PointAction != null && m_ActionsHooked)
                    m_PointAction.action.performed += m_ActionCallback;
            }
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2">2D motion vector
        /// </see> used for sending <see cref="AxisEventData"/> events.
        /// </summary>
        public InputActionProperty move
        {
            get { return m_MoveAction; }
            set
            {
                if (m_MoveAction != null && m_ActionsHooked)
                    m_MoveAction.action.performed -= m_ActionCallback;
                m_MoveAction = value;
                if (m_PointAction != null && m_ActionsHooked)
                    m_PointAction.action.performed += m_ActionCallback;
            }
        }

        public InputActionProperty submit
        {
            get { return m_SubmitAction; }
            set
            {
                if (m_SubmitAction != null && m_ActionsHooked)
                    m_SubmitAction.action.performed -= m_ActionCallback;
                m_SubmitAction = value;
                if (m_SubmitAction != null && m_ActionsHooked)
                    m_SubmitAction.action.performed += m_ActionCallback;
            }
        }

        public InputActionProperty cancel
        {
            get { return m_CancelAction; }
            set
            {
                if (m_CancelAction != null && m_ActionsHooked)
                    m_CancelAction.action.performed -= m_ActionCallback;
                m_CancelAction = value;
                if (m_CancelAction != null && m_ActionsHooked)
                    m_CancelAction.action.performed += m_ActionCallback;
            }
        }

        public InputActionProperty leftClick;

        public InputActionProperty middleClick;

        public InputActionProperty rightClick;

        public InputActionProperty scroll;

        public void OnDestroy()
        {
            UnhookActions();

            if (m_ActionQueue != null)
            {
                m_ActionQueue.Dispose();
                m_ActionQueue = null;
            }
        }

        public void OnEnable()
        {
            base.OnEnable();
            HookActions();
        }

        public void OnDisable()
        {
            UnhookActions();
        }

        public override void Process()
        {
            foreach (var entry in m_ActionQueue)
            {
                var action = entry.action;
                Debug.Assert(action != null);
                if (action == null)
                    continue;

                if (action == m_PointAction)
                {
                    var control = entry.control;
                    var device = control != null ? control.device : null;
                    var pointer = device as Pointer;
                    var pointerId = pointer != null ? pointer.pointerId.ReadValue() : 0;

                    // Initialize event.
                    var eventData = GetOrCreateCachedPointerEvent();
                    eventData.pointerId = pointerId;
                    eventData.position = entry.ReadValue<Vector2>();
                    PerformRaycast(eventData);

                    // Fire events.
                    HandlePointerExitAndEnter(eventData, eventData.pointerCurrentRaycast.gameObject);

                    eventData.Reset();
                }
                else if (action == m_MoveAction)
                {
                    // Don't send move events if disabled in the EventSystem.
                    if (!eventSystem.sendNavigationEvents)
                        continue;

                    throw new NotImplementedException();
                }
            }
            m_ActionQueue.Clear();
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
        private InputActionProperty m_ScrollAction;

        [NonSerialized] private bool m_ActionsHooked;
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

        /// <summary>
        /// If the left click button is currently held, this is the button control.
        /// </summary>
        [NonSerialized] private InputControl m_LeftClickControl;
        [NonSerialized] private InputControl m_RightClickControl;
        [NonSerialized] private InputControl m_MiddleClickControl;
    }
}
