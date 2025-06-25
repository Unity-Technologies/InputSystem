using UnityEngine.Events;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A simple component that deactivates a target when action is performed.
    /// </summary>
    public class InvokeUnityEvent : MonoBehaviour
    {
        [Tooltip("The input action that triggers the Unity event when performed.")]
        public InputActionReference action;

        [Tooltip("The Unity event to be invoked when action is performed.")]
        public UnityEvent onPerformed;

        private void OnEnable()
        {
            // Register callback when component is enabled
            if (action != null && action.action != null)
                action.action.performed += OnActionPerformed;
        }

        private void OnDisable()
        {
            // Unregister callback when component is disabled
            if (action != null && action.action != null)
                action.action.performed -= OnActionPerformed;
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            onPerformed?.Invoke();
        }
    }
}
