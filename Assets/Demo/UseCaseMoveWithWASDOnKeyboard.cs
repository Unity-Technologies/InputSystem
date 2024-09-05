using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.Serialization;

namespace UseCases
{
    public class UseCaseMoveWithWASDOnKeyboard : UseCase
    {
        #region How to currently do it
        // Most similar way to do it today would be to poll state:
        //
        // void Update()
        // {
        //    var x = -1.0f * Keyboard.current.keyA.value + Keyboard.current.keyD.value;
        //    var y = -1.0f * Keyboard.current.keyA.value + Keyboard.current.keyD.value;
        //    moveDirection = new Vector2(x, y);
        // }
        //
        // OR
        //
        // var action = new InputAction("Move", InputActionType.PassThrough);
        // action.AddCompositeBinding("Dpad")
        // .With("Up", "<Keyboard>/w")
        // .With("Down", "<Keyboard>/s")
        // .With("Left", "<Keyboard>/a")
        // .With("Right", "<Keyboard>/d");
        // action.performed += (ctx) => moveDirection = ctx.ReadValue<Vector>());
        // action.Enable();
        #endregion
        
        private IDisposable m_Subscription;
    
        private void OnEnable()
        {
            m_Subscription = Combine.Composite(Keyboard.A, Keyboard.D, Keyboard.S, Keyboard.W)
                .Subscribe(v => moveDirection = v);
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
        }
    }
}
