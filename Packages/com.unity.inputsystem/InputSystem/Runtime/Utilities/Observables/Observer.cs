using System;

namespace UnityEngine.InputSystem.Utilities
{
    internal class Observer<TValue> : IObserver<TValue>
    {
        private Action<TValue> m_OnNext;
        private Action m_OnCompleted;

        public Observer(Action<TValue> onNext, Action onCompleted = null)
        {
            m_OnNext = onNext;
            m_OnCompleted = onCompleted;
        }

        public void OnCompleted()
        {
            m_OnCompleted?.Invoke();
        }

        public void OnError(Exception error)
        {
            Debug.LogException(error);
        }

        public void OnNext(TValue evt)
        {
            m_OnNext?.Invoke(evt);
        }
    }
}
