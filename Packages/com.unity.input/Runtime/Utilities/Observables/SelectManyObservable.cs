using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilities
{
    internal class SelectManyObservable<TSource, TResult> : IObservable<TResult>
    {
        private readonly IObservable<TSource> m_Source;
        private readonly Func<TSource, IEnumerable<TResult>> m_Filter;

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> filter)
        {
            m_Source = source;
            m_Filter = filter;
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            return m_Source.Subscribe(new Select(this, observer));
        }

        private class Select : IObserver<TSource>
        {
            private SelectManyObservable<TSource, TResult> m_Observable;
            private readonly IObserver<TResult> m_Observer;

            public Select(SelectManyObservable<TSource, TResult> observable, IObserver<TResult> observer)
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

            public void OnNext(TSource evt)
            {
                foreach (var result in m_Observable.m_Filter(evt))
                    m_Observer.OnNext(result);
            }
        }
    }
}
