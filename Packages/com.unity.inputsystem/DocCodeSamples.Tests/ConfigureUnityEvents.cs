namespace DocCodeSamples.Tests.ConfigureUnityEvents_ManualEnable
{
    #region manualEnableSingleton
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Example script demonstrating manually enabling actions instead of using the
    /// default project-wide action map.
    /// </summary>
    public class MyPlayerScript : MonoBehaviour
    {
        PlayerInput playerInput;

        void Start()
        {
            playerInput = GetComponent<PlayerInput>();
            InputSystem.actions.Disable();
            playerInput.currentActionMap?.Enable();
        }
    }
    #endregion
}

namespace DocCodeSamples.Tests.ConfigureUnityEvents_SendMessages
{
    #region sendMessages
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Example script demonstrating the <c>PlayerInput</c> "Send Messages" behavior.
    /// </summary>
    public class MyPlayerScript : MonoBehaviour
    {
        // "jump" action becomes "OnJump" method.

        /// <summary>
        /// Called by <c>PlayerInput</c> when the "jump" action is triggered.
        /// </summary>
        // If you're not interested in the value from the control that triggers the action, use a method without arguments.
        public void OnJump()
        {
            // your Jump code here
        }

        /// <summary>
        /// Called by <c>PlayerInput</c> when the "move" action is triggered.
        /// </summary>
        /// <param name="value">Value of the control that triggered the action.</param>
        // If you are interested in the value from the control that triggers an action, you can declare a parameter of type InputValue.
        public void OnMove(InputValue value)
        {
            // Read value from control. The type depends on what type of controls.
            // the action is bound to.
            var v = value.Get<Vector2>();

            // IMPORTANT:
            // The given InputValue is only valid for the duration of the callback. Storing the InputValue references somewhere and calling Get<T>() later does not work correctly.
        }
    }
    #endregion
}

namespace DocCodeSamples.Tests.ConfigureUnityEvents_InvokeUnityEvents
{
    #region invokeUnityEvents
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Example script demonstrating the <c>PlayerInput</c> "Invoke Unity Events" behavior.
    /// </summary>
    public class MyPlayerScript : MonoBehaviour
    {
        /// <summary>
        /// Called when the "fire" action is triggered.
        /// </summary>
        /// <param name="context">Context for the triggered action.</param>
        public void OnFire(InputAction.CallbackContext context)
        {
        }

        /// <summary>
        /// Called when the "move" action is triggered.
        /// </summary>
        /// <param name="context">Context for the triggered action.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
        }
    }
    #endregion
}
