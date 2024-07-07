using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Implement a dependency graph walker
    
    // This is a reader associated with an underlying stream
    public readonly struct SubscriptionReader<T> : IDisposable, IEnumerable<T> 
        where T : struct
    {
        private readonly Context.StreamContext<T> m_StreamContext;

        internal SubscriptionReader([NotNull] Context.StreamContext<T> streamContext)
        {
            m_StreamContext = streamContext;
        }

        internal SubscriptionReader([NotNull] Context context, Usage usage)
            : this(context.GetOrCreateStreamContext<T>(usage))
        { }

        public T Read()
        {
            return m_StreamContext.Stream?.GetLast() ?? default; // TODO Can we do better?
        }

        public string Describe()
        {
            return string.Empty;
        }
        
        public void Dispose()
        {
            // TODO Fix
        }

        public NativeSlice<T>.Enumerator GetEnumerator() => m_StreamContext.GetEnumerator(); // TODO If consistent type we may use interface constraint 
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}