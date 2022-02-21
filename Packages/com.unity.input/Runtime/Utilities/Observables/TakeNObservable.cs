using System;

namespace UnityEngine.InputSystem.Utilities
{
    internal class TakeNObservable<TValue> : IObservable<TValue>
    {
        private IObservable<TValue> m_Source;
        private int m_Count;

        public TakeNObservable(IObservable<TValue> source, int count)
        {
            m_Source = source;
            m_Count = count;
        }

        public IDisposable Subscribe(IObserver<TValue> observer)
        {
            return m_Source.Subscribe(new Take(this, observer));
        }

        private class Take : IObserver<TValue>
        {
            private IObserver<TValue> m_Observer;
            private int m_Remaining;

            public Take(TakeNObservable<TValue> observable, IObserver<TValue> observer)
            {
                m_Observer = observer;
                m_Remaining = observable.m_Count;
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
                if (m_Remaining <= 0)
                    return;

                m_Remaining--;
                m_Observer.OnNext(evt);

                if (m_Remaining == 0)
                {
                    m_Observer.OnCompleted();
                    m_Observer = default;
                }
            }
        }
    }
}
