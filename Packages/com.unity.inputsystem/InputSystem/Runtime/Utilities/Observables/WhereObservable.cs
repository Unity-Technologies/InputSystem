using System;

namespace UnityEngine.InputSystem.Utilities
{
    internal class WhereObservable<TValue> : IObservable<TValue>
    {
        private readonly IObservable<TValue> m_Source;
        private readonly Func<TValue, bool> m_Predicate;

        public WhereObservable(IObservable<TValue> source, Func<TValue, bool> predicate)
        {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IDisposable Subscribe(IObserver<TValue> observer)
        {
            return m_Source.Subscribe(new Where(this, observer));
        }

        private class Where : IObserver<TValue>
        {
            private WhereObservable<TValue> m_Observable;
            private readonly IObserver<TValue> m_Observer;

            public Where(WhereObservable<TValue> observable, IObserver<TValue> observer)
            {
                m_Observable = observable;
                m_Observer = observer;
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
                if (m_Observable.m_Predicate(evt))
                    m_Observer.OnNext(evt);
            }
        }
    }
}
