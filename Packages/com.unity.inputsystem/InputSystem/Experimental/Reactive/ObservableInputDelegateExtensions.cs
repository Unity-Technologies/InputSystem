using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    public static class ObservableInputDelegateExtensions
    {
        // Internal helper class that wraps an Action as an IObserver
        private sealed class NextObserverDelegateWrapper<T> : IObserver<T>
        {
            private readonly Context m_Context;
            private readonly Action<T> m_Action;
            private int m_Priority;

            public NextObserverDelegateWrapper([NotNull] Context context, [NotNull] Action<T> action)
            {
                m_Context = context;
                m_Action = action;
            }

            public void OnCompleted()
            {
                // ignored
            }

            public void OnError(Exception error)
            {
                Debug.LogException(error);   
            }
            
            public void OnNext(T value)
            {
                m_Action(value);   
            }
        }
        
        // TODO This should not be like this, fix. Only to PoC priority-based consumption
        private sealed class NextObserverDelegateWrapperWithSettings<T> : IObserver<T>
        {
            private readonly Context m_Context;
            private readonly Action<T> m_Action;
            private Action m_Deferred;
            private T m_Stored;
            private int m_Priority;

            public NextObserverDelegateWrapperWithSettings([NotNull] Context context, [NotNull] Action<T> action, int priority)
            {
                m_Context = context;
                m_Action = action;
                m_Priority = priority;
                if (m_Priority != 0)
                    m_Deferred = () => m_Action(m_Stored);
            }

            public void OnCompleted()
            {
                // ignored
            }

            public void OnError(Exception error)
            {
                Debug.LogException(error);   
            }

            private void ForwardOnNext(T value)
            {
                m_Action(value);
            }

            public void OnNext(T value) // TODO This should move to observer base ?!
            {
                // Forward directly unless priority is set, in which case we instead defer the invocation of the
                // callback with an associated priority. In order for deferral to work we need to capture value.
                if (m_Priority == 0)
                {
                    m_Action(value);
                    return;
                }
                m_Stored = value;
                m_Context.Defer(m_Deferred, m_Priority);
            }
        }

        /// <summary>
        /// Subscribe to an observable but invoke a delegate instead of an IObservable.
        /// </summary>
        /// <param name="observable">The observable to subscribe to.</param>
        /// <param name="action">The delegate to be invoked upon data received.</param>
        /// <param name="context"></param>
        /// <param name="priority"></param>
        /// <typeparam name="T">The data type.</typeparam>
        /// <typeparam name="TSource">The observable type.</typeparam>
        /// <returns>Disposable subscription.</returns>
        public static IDisposable Subscribe<T>(this IObservableInput<T> observable, Action<T> action, Context context, int priority = 0) 
            where T : struct
        {
            // TODO: This shouldn't be unique to the action wrapper, it should apply to any terminating or partial end-point subscription.
            //       However, we do not need it in every node, so we might as well utilize it in termination nodes.
            if (priority == 0)
                return observable.Subscribe(context, new NextObserverDelegateWrapper<T>(context, action));
            return observable.Subscribe(context, new NextObserverDelegateWrapperWithSettings<T>(context, action, priority));
        }
        
        // TODO We may do some syntactic sugar here if we want with an TrySubscribe that allows for avoiding
        //      ?. problem with Unity objects and still not fail if observable is null.
        
        public static IDisposable TrySubscribe<T>(this IObservableInput<T> observable, Action<T> action, int priority = 0) 
            where T : struct
        {
            if (observable != null)
            {
                try
                {
                    return Subscribe(observable, action, Context.instance, priority);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }    
            }
            
            return null;
        } 
        
        public static IDisposable Subscribe<T>(this IObservableInput<T> observable, Action<T> action, int priority = 0) 
            where T : struct
        {
            return Subscribe(observable, action, Context.instance, priority);
        }
        
        public static IDisposable Subscribe<T, TCollection>(this IObservableInput<T> observable, Action<T> action, TCollection group) 
            where T : struct
            where TCollection : ICollection<IDisposable>
        {
            var subscription = Subscribe(observable, action, Context.instance);
            group.Add(subscription);
            return subscription;
        }
        
        // TODO If we have C#11 we might do public static IDisposable Subscribe<T>(this IObservableInput<T> observable, ref T field)
        
        /*public static IDisposable Subscribe<T, TSource>(this TSource observable, Action<T> action, Context context = null) 
            where T : struct 
            where TSource : IObservableInputNode<T>
        {
            return observable.Subscribe(context, new NextObserverDelegateWrapper<T>(action));
        }*/

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
                m_OnCompleted?.Invoke();
            }

            public void OnError(Exception error)
            {
                m_OnError?.Invoke(error);
            }

            public void OnNext(T value)
            {
                m_OnNext?.Invoke(value);
            }
        }
        
        public static IDisposable Subscribe<T, TSource>(this TSource observable, 
            Action onCompleted = null, Action<Exception> onError = null, Action<T> onNext = null,
            Context context = null)
            where T : struct 
            where TSource : IObservableInputNode<T>
        {
            return observable.Subscribe(context, new ObserverDelegateWrapper<T>(onCompleted, onError, onNext));
        }
    }
}