using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents an observable binding source providing input of type <typeparamref name="T"/> which is associated
    /// with a specific end-point but not with a context.
    /// </summary>
    /// <typeparam name="T">The observable type.</typeparam>
    public interface IObservableInput<out T> : IObservable<T>, IDependencyGraphNode 
        where T : struct
    {
        /// <summary>
        /// Subscribes to the given source within context <paramref name="context"/>
        /// </summary>
        /// <param name="context">The associated context.</param>
        /// <param name="observer">The observer to receive data when available.</param>
        /// <returns>Disposable subscription object.</returns>
        public IDisposable Subscribe([NotNull] Context context, IObserver<T> observer); // TODO If we allow the subscription type to implement IDisposable but be a specific type we could avoid indirection
     
        // TODO Consider an alternative API for which we can do jobs without indirection overhead for selected use-cases. 
        // TODO public StreamSubscription<T> Subscribe([NotNull] context, ObservableInput<T> source)
    }
}