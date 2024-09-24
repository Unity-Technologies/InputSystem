using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Similar to <see cref="IForwardReceiver{T}"/> but additionally forwards a numerical index enumerating the
    /// index of the forwarded input.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    interface IIndexedForwardReceiver<in T> : IObserver<T>
        where T : struct
    {
        void ForwardOnCompleted(int index);
        void ForwardOnError(int index, Exception error);
        void ForwardOnNext(int index, T value);
    }
}