using System;
using UnityEngine.Events;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A simple component that deactivates a target when action is performed.
    /// </summary>
    /// <remarks>
    /// Note that this implementation do not handle action changing during run-time.
    /// </remarks>
    public class InvokeUnityEvent : MonoBehaviour
    {
        [Tooltip("The input action that triggers the Unity event when performed.")]
        [SerializeField] private InputActionReference m_Action;

        [Tooltip("The Unity event to be invoked when action is performed.")]
        [SerializeField] private UnityEvent m_OnPerformed = new UnityEvent();

        private Action<InputAction.CallbackContext> m_OnActionPerformed;
        private bool m_HaveRegisteredCallback = false;

        /// <summary>
        /// Sets/gets the associated action that triggers the event.
        /// </summary>
        /// <remarks>Registration and unregistration for event forwarding is handled automatically.</remarks>
        public InputActionReference action
        {
            get => m_Action;
            set
            {
                if (m_Action == value)
                    return;

                Unregister();
                m_Action = value;
                Register();
            }
        }

        /// <summary>
        /// Access or set the Unity event to be triggered when the action is performed.
        /// </summary>
        public UnityEvent onPerformed
        {
            get => m_OnPerformed;
            set
            {
                if (m_OnPerformed == value)
                    return;

                // If we just change forwarding target there is no need to do any action.
                // If this is the first time its set or we set it to null we invoke registration logic.
                var manageRegistration = m_OnPerformed == null || value == null;
                if (manageRegistration)
                    Unregister();
                m_OnPerformed = value;
                if (manageRegistration)
                    Register();
            }
        }

        private void Awake()
        {
            // Cache action ensuring no memory allocation occurs on registration of callback.
            m_OnActionPerformed = OnActionPerformed;
        }

        private void OnEnable()
        {
            // Register callback when component is enabled.
            Register();
        }

        private void OnDisable()
        {
            // Unregister callback when component is disabled.
            Unregister();
        }

        private void Register()
        {
            if (!m_HaveRegisteredCallback && m_OnPerformed != null && m_Action != null && m_Action.action != null)
            {
                action.action.performed += m_OnActionPerformed;
                m_HaveRegisteredCallback = true;
            }
        }

        private void Unregister()
        {
            if (m_HaveRegisteredCallback && action != null && action.action != null)
            {
                action.action.performed -= m_OnActionPerformed;
                m_HaveRegisteredCallback = false;
            }
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            // Invoke associated callback when performed.
            onPerformed?.Invoke();
        }
    }
}
