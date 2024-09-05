using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace UseCases
{
    public class UseCaseLocalMultiplayer : UseCase
    {
        #region How to currently do it
        // TODO
        #endregion

        [Tooltip("If non-zero, specifies the ID of the player for which for filter bindings")]
        private uint m_PlayerId; // TODO Unity doesn't really support a proper way to react to this changing unfortunately
        
        private BindableInput<Vector2> m_Move; // TODO Should be a scriptable object or extracted from one
        
        private IDisposable m_Subscription;

        private void OnEnable()
        {
            m_Subscription = m_Move?.Player(m_PlayerId).Subscribe(v => moveDirection = v);
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
        }
    }
}
