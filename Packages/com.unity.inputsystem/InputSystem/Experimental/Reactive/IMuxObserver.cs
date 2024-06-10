using System;

namespace UnityEngine.InputSystem.Experimental
{
    internal interface IMuxObserver<in T> where T : struct
    {
        void OnCompleted(int sourceIndex);
        void OnError(int sourceIndex, Exception error);
        void OnNext(int sourceIndex, T value);
    }
}