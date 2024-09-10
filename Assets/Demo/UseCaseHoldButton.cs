using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace UseCases
{
    public class UseCaseHoldButton : UseCase
    {
        #region How to currently do it
        // TODO Enable feature in settings, still not working fully as expected, no fine-grained control
        #endregion
        
        private IDisposable m_Subscription0, m_Subscription1, m_Subscription2;
    
        private void OnEnable()
        {
            m_Subscription0 = Keyboard.C.Pressed()
                .Subscribe(@event => Debug.Log("Pressed(C)"));
            m_Subscription1 = Keyboard.C.Released()
                .Subscribe(@event => Debug.Log("Released(C)"));
            m_Subscription2 = Keyboard.C.Held(duration: TimeSpan.FromSeconds(1.0))
                .Subscribe(@event => Debug.Log("Held(C) for 1 second"));
        }

        private void OnDisable()
        {
            m_Subscription0?.Dispose();
            m_Subscription1?.Dispose();
            m_Subscription2?.Dispose();

        }
    }
}
