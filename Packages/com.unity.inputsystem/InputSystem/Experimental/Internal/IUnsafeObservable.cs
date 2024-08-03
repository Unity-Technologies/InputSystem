using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IUnsafeObserver<T>
    {
        /// <summary>
        /// Returns a delegate to use when invoking the observer.
        /// </summary>
        /// <returns>Unsafe delegate instance.</returns>
        //UnsafeDelegate<T> GetDelegate();
            
        //void OnNext(T value, )
    }
    
    public interface IUnsafeObservable<T>
    {
        /// <summary>
        /// Subscribes <paramref name="observer"/> to output from this observable.
        /// </summary>
        /// <param name="context">The associated context.</param>
        /// <param name="observer">Delegate to be invoked.</param>
        /// <returns>Subscription object that may be disposed to cancel the subscription.</returns>
        UnsafeSubscription Subscribe<TSource>([NotNull] Context context, TSource observer) 
            where TSource : IUnsafeObserver<T>;
    }
    
    public struct UnsafeSubscription : IDisposable
    {
        private IntPtr m_EventHandler; // TODO Consider if this should be a pointer to event handler to modify state?!
        private readonly UnsafeDelegate.Callback m_Callback;
            
        internal UnsafeSubscription(IntPtr eventHandler, UnsafeDelegate.Callback callback) 
        {
            m_EventHandler = eventHandler;
            m_Callback = callback;
        }
            
        public void Dispose()
        {
            UnsafeDelegate.Remove(ref m_EventHandler, m_Callback);
        }
    }
}