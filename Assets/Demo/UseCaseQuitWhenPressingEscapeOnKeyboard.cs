using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace UseCases
{
    public class UseCaseQuitWhenPressingEscapeOnKeyboard : UseCase
    {
        #region How to currently do it
        // Most similar way to do it today would be to poll state:
        //
        // void Update()
        // {
        //      if (Keyboard.current.escapeKey.wasPressedThisFrame())
        //          Quit();
        // }
        #endregion
        
        private IDisposable m_Subscription;
    
        private void OnEnable()
        {
            m_Subscription = Keyboard.Escape.Pressed().Subscribe(pressEvent => Quit());
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
        }
    }
}
