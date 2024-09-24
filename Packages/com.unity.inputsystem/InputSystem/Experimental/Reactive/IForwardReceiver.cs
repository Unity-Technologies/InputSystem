using System;

namespace UnityEngine.InputSystem.Experimental
{
    internal interface IForwardReceiver<in T> 
        where T : struct
    {
        void ForwardOnCompleted();
        void ForwardOnError(Exception error);
        void ForwardOnNext(T value);
    }
}