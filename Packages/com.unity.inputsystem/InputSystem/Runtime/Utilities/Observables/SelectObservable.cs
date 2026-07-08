using System;

namespace UnityEngine.InputSystem.LowLevel
{
    internal class SelectObservable<TSource, TResult> : IObservable<TResult>
    {
        private readonly IObservable<TSource> m_Source;
        private readonly Func<TSource, TResult> m_Filter;

        public SelectObservable(IObservable<TSource> source, Func<TSource, TResult> filter)
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
            private SelectObservable<TSource, TResult> m_Observable;
            private readonly IObserver<TResult> m_Observer;

            public Select(SelectObservable<TSource, TResult> observable, IObserver<TResult> observer)
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
                var result = m_Observable.m_Filter(evt);
                m_Observer.OnNext(result);
            }
        }
    }
}
