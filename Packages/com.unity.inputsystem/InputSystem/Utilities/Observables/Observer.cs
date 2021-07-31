using System;

namespace UnityEngine.InputSystem.Utilities
{
    internal class Observer<TValue> : IObserver<TValue>
    {
        private Action<TValue> m_Action;

        public Observer(Action<TValue> action)
        {
            m_Action = action;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            Debug.LogException(error);
        }

        public void OnNext(TValue evt)
        {
            m_Action?.Invoke(evt);
        }
    }
}
