namespace DocCodeSamples.Tests.ConfigureUnityEvents_ManualEnable
{
    #region manualEnableSingleton
    using UnityEngine;
    using UnityEngine.InputSystem;

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

    public class MyPlayerScript : MonoBehaviour
    {
        // "jump" action becomes "OnJump" method.

        // If you're not interested in the value from the control that triggers the action, use a method without arguments.
        public void OnJump()
        {
            // your Jump code here
        }

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

    public class MyPlayerScript : MonoBehaviour
    {
        public void OnFire(InputAction.CallbackContext context)
        {
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
        }
    }
    #endregion
}
