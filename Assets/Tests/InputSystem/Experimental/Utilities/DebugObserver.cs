using System;
using UnityEngine;

namespace Tests.InputSystem
{
    /// <summary>
    /// An observer that prints observed values to the console.
    /// </summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    public class DebugObserver<T> : IObserver<T> where T : struct
    {
        public void OnCompleted() => Debug.Log("OnCompleted");
        public void OnError(Exception error) => Debug.Log("OnError: " + error);
        public void OnNext(T value) => Debug.Log("OnNext: " + value);
    }
}