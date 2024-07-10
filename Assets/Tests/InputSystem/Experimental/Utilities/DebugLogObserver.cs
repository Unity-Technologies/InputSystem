using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem
{
    /// <summary>
    /// An observer that prints observed values to the console.
    /// </summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    public sealed class DebugLogObserver<T> : IObserver<T> where T : struct
    {
        private readonly string m_Prefix;
        
        public DebugLogObserver([NotNull] string prefix)
        {
            m_Prefix = prefix;
        }
        
        public void OnCompleted() => Debug.Log($"{m_Prefix}.OnCompleted");
        public void OnError(Exception error) => Debug.Log($"{m_Prefix}.OnError: {error.Message}");
        public void OnNext(T value) => Debug.Log($"{m_Prefix}.OnNext: {value}");
    }

    /// <summary>
    /// Extension methods related to <see cref="DebugLogObserver{T}"/>.
    /// </summary>
    public static class DebugObserverExtensions
    {
        /// <summary>
        /// Subscribes a new <see cref="DebugLogObserver{T}"/> instance to <paramref name="source"/>. 
        /// </summary>
        /// <param name="source">The source to subscribe to.</param>
        /// <param name="context">The associated context.</param>
        /// <typeparam name="T">The associated data type.</typeparam>
        /// <returns><c>IDisposable</c> that cancels the subscription.</returns>
        public static IDisposable DebugLog<T>(this IObservableInput<T> source, Context context = null)
            where T : struct
        {
            return source.Subscribe(context, new DebugLogObserver<T>(source.displayName));            
        }
    }
}