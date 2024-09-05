using System;
using UnityEngine.InputSystem.Experimental;
using Keyboard = UnityEngine.InputSystem.Experimental.Devices.Keyboard;

namespace UseCases
{
    public class UseCaseFireWhileHoldingSpaceOnKeyboard : UseCase
    {
        #region How to currently do it
        // Most similar way to do it today would be to poll state:
        //
        // void Update()
        // {
        //      // Note: polling so order of operations matter
        //      // Note: need to check with default press point since its a float (HID keys are binary)
        //      isFiring = UnityEngine.InputSystem.Keyboard.current.spaceKey.value > InputSystem.settings.defaultButtonPressPoint;
        // }
        //
        // OR
        //
        // void OnEnable()
        // {
        //     // Note: 
        //     var action = new InputAction("Fire", InputActionType.PassThrough, "<Keyboard>/space");
        //     action.performed += context => { isFiring = context.ReadValue<float>() >= InputSystem.settings.defaultButtonPressPoint; };
        //     action.Enable();
        // ≠
        #endregion
        
        private IDisposable m_Subscription;
    
        private void OnEnable()
        {
            m_Subscription = Keyboard.Space.Subscribe(v => isFiring = v);
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
        }

        void Update()
        {
            
        }
    }
}
