using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// An <see cref="IObserver{T}"/> that forwards directly to a <see cref="IForwardReceiver{T}"/> implementation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TReceiver">The receiver type.</typeparam>
    internal sealed class ForwardingObserver<T, TReceiver> : IObserver<T> 
        where TReceiver : IForwardReceiver<T>
        where T : struct
    {
        private readonly TReceiver m_Receiver;
        
        public ForwardingObserver(TReceiver receiver)
        {
            m_Receiver = receiver;
        }
        
        public void OnCompleted() => m_Receiver.ForwardOnCompleted();
        public void OnError(Exception error) => m_Receiver.ForwardOnError(error);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnNext(T value) => m_Receiver.ForwardOnNext(value);
    }
}