using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;

namespace UnityEngine.InputSystem.Experimental
{
    /*public interface IUnsafeObserver<T>
    {
        /// <summary>
        /// Returns a delegate to use when invoking the observer.
        /// </summary>
        /// <returns>Unsafe delegate instance.</returns>
        //UnsafeDelegate<T> GetDelegate();
            
        //void OnNext(T value, )
    }*/
    
    public interface IUnsafeObservable<T> where T : struct
    {
        /// <summary>
        /// Subscribes <paramref name="observer"/> to output from this observable.
        /// </summary>
        /// <param name="context">The associated context.</param>
        /// <param name="observer">Unsafe delegate to be invoked when data is available.</param>
        /// <returns>Subscription object that may be disposed to cancel the subscription.</returns>
        UnsafeSubscription Subscribe([NotNull] Context context, UnsafeDelegate<T> observer);
    }
}