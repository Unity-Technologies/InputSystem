using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// An implementation of <see cref="IIndexedForwardReceiver{T}"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TReceiver">The receiver type</typeparam>
    internal sealed class IndexedForwardingObserver<T, TReceiver> : IObserver<T> 
        where TReceiver : IIndexedForwardReceiver<T>
        where T : struct
    {
        private readonly TReceiver m_Receiver;
        private readonly int m_Index;
        
        public IndexedForwardingObserver(int index, TReceiver receiver)
        {
            m_Receiver = receiver;
            m_Index = index;
        }
        
        public void OnCompleted()
        {
            m_Receiver.ForwardOnCompleted(m_Index);
        }

        public void OnError(Exception error)
        {
            m_Receiver.ForwardOnError(m_Index, error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnNext(T value)
        {
            m_Receiver.ForwardOnNext(m_Index, value);
        }
    }
}