// TODO In general this solution also suffers from a problem that needs a solution which is related to negative transitions on the state graph, e.g. not counting shortcut if some other key is down. So likely this needs to be expressed or be checked. At least it might imply that shorcuts/chords should be co-evaluated.

using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace UseCases
{
    public class UseCaseShortcuts : UseCase
    {
        public bool ShowPressEvents = true;
        public bool ShowReleaseEvents = false;
        
        #region How to currently do it
        // TODO Enable feature in settings, still not working fully as expected, no fine-grained control
        #endregion
        
        private IDisposable m_Subscription0, m_Subscription1, m_Subscription2, m_Subscription3;
    
        private void OnEnable()
        {
            if (ShowPressEvents)
                m_Subscription0 = Keyboard.C.Pressed().Subscribe(@event => Debug.Log("Pressed(C)"), priority: 1);
            if (ShowReleaseEvents)
                m_Subscription1 = Keyboard.C.Released().Subscribe(@event => Debug.Log("Released(C)"), priority: 1);
            m_Subscription2 = Combine.Shortcut(Keyboard.LeftCtrl, Keyboard.C).Pressed().Subscribe(@event => Debug.Log("Pressed(Ctrl+C)"), priority: 2);
            m_Subscription3 = Combine.Shortcut(Keyboard.LeftShift, Keyboard.C).Pressed().Subscribe(@event => Debug.Log("Pressed(Shift+C)"), priority: 2);
            // Keyboard.LeftShift.Shortcut(Keyboard.C);
        }

        private void OnDisable()
        {
            m_Subscription0?.Dispose();
            m_Subscription1?.Dispose();
            m_Subscription2?.Dispose();
            m_Subscription3?.Dispose();
        }
    }
}
