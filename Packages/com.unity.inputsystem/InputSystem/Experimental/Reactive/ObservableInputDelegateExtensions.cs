using System;

namespace UnityEngine.InputSystem.Experimental
{
    public static class ObservableInputDelegateExtensions
    {
        // Internal helper class that wraps an Action as an IObserver
        private sealed class SimpleObserverDelegateWrapper<T> : IObserver<T>
        {
            private readonly Action<T> m_Action;
            public SimpleObserverDelegateWrapper(Action<T> action) => m_Action = action;
            public void OnCompleted() { } // ignored
            public void OnError(Exception error) => Debug.LogException(error);
            public void OnNext(T value) => m_Action(value);
        }
        
        public static IDisposable Subscribe<T, TSource>(this TSource observable, Action<T> action) 
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return observable.Subscribe(new SimpleObserverDelegateWrapper<T>(action));
        }
        
        public static IDisposable Do<T, TSource>(this TSource observable, Action<T> action) 
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return observable.Subscribe(new SimpleObserverDelegateWrapper<T>(action));
        }

        // Internal helper class that wraps an Actions as an IObserver
        private sealed class ObserverDelegateWrapper<T> : IObserver<T>
        {
            private readonly Action m_OnCompleted;
            private readonly Action<Exception> m_OnError;
            private readonly Action<T> m_OnNext;

            public ObserverDelegateWrapper(Action mOnCompleted, Action<Exception> onError, Action<T> onNext)
            {
                m_OnCompleted = mOnCompleted;
                m_OnError = onError;
                m_OnNext = onNext;
            }

            public void OnCompleted()
            {
                m_OnCompleted(); 
            }

            public void OnError(Exception error)
            {
                m_OnError(error);
            }

            public void OnNext(T value)
            {
                m_OnNext(value);
            }
        }
        
        public static IDisposable Subscribe<T, TSource>(this TSource observable,
            Action onCompleted, Action<Exception> onError, Action<T> onNext)
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return observable.Subscribe(new ObserverDelegateWrapper<T>(onCompleted, onError, onNext));
        }
    }
}