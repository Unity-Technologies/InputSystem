using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace UnityEngine.InputSystem.Experimental.Internal
{
    /// <summary>
    /// A simple thread-safe object pool intended for object reuse to avoid GC overhead.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    internal sealed class ObjectPool<T> 
        where T : class, new()
    {
        // Potential future extensions (if needed):
        // TODO Introduce a maxSize and allow GC to collect objects if exceeded.
        // TODO In editor or debug build record object pool max size for profiling or guided optimization 
        
        private static ObjectPool<T> _shared;
        private readonly ConcurrentQueue<T> m_Pool;
        
        private ObjectPool()
        {
            m_Pool = new ConcurrentQueue<T>();
        }
        
        // Prevents a race condition at construction. Requires C# 7 and .NET 6.
        // If only eager, private static readonly ObjectPool<T> instance = new ObjectPool<T>(); would have
        // been sufficient.
        private static readonly Lazy<ObjectPool<T>> Lazy = new(() => new ObjectPool<T>());

        /// <summary>
        /// Returns a reference to the shared pool instance.
        /// </summary>
        public static ObjectPool<T> shared => Lazy.Value;
        
        /// <summary>
        /// Rents an object from the pool. If no object is available in the pool a new object is constructed.
        /// </summary>
        /// <returns>Object instance. Never null.</returns>
        public T Rent()
        {
            if (m_Pool.TryDequeue(out T result))
            {
                return result;
            }

            return new T();
        }

        /// <summary>
        /// Returns an object instance to the object pool.
        /// </summary>
        /// <param name="value">The object instance to be returned.</param>
        public void Return(T value)
        {
            m_Pool.Enqueue(value);
        }
    }
}