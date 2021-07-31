using System;

namespace UnityEngine.InputSystem.Utilities
{
    internal class AutoDisposeObserver<TValue> : IObserver<TValue>
    {
        public IDisposable disposable;
        private Action<TValue> m_Action;

        public AutoDisposeObserver(Action<TValue> action)
        {
            m_Action = action;
        }

        public void OnCompleted()
        {
            disposable.Dispose();
        }

        public void OnError(Exception error)
        {
            Debug.LogException(error);
            OnCompleted();
        }

        public void OnNext(TValue value)
        {
            m_Action(value);
        }
    }
}
