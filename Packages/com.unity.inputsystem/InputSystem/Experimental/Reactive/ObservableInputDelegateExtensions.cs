using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    public static class ObservableInputDelegateExtensions
    {
        // Internal helper class that wraps an Action as an IObserver
        private sealed class NextObserverDelegateWrapper<T> : IObserver<T>
        {
            private readonly Action<T> m_Action;
            public NextObserverDelegateWrapper([NotNull] Action<T> action) => m_Action = action;
            public void OnCompleted() { } // ignored
            public void OnError(Exception error) => Debug.LogException(error);
            public void OnNext(T value) => m_Action(value);
        }
        
        /// <summary>
        /// Subscribe to an observable but invoke a delegate instead of an IObservable.
        /// </summary>
        /// <param name="observable">The observable to subscribe to.</param>
        /// <param name="action">The delegate to be invoked upon data received.</param>
        /// <typeparam name="T">The data type.</typeparam>
        /// <typeparam name="TSource">The observable type.</typeparam>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable Subscribe<T, TSource>(this TSource observable, Action<T> action, Context context = null) 
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return observable.Subscribe(context, new NextObserverDelegateWrapper<T>(action));
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
                if (m_OnCompleted != null)
                    m_OnCompleted(); 
            }

            public void OnError(Exception error)
            {
                if (m_OnError != null)
                    m_OnError(error);
            }

            public void OnNext(T value)
            {
                if (m_OnNext != null)
                    m_OnNext(value);
            }
        }
        
        public static IDisposable Subscribe<T, TSource>(this TSource observable, 
            Action onCompleted = null, Action<Exception> onError = null, Action<T> onNext = null,
            Context context = null)
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return observable.Subscribe(context, new ObserverDelegateWrapper<T>(onCompleted, onError, onNext));
        }
    }
}