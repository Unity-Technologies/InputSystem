using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tests.InputSystem
{
    internal class ListObserver<T> : IObserver<T>
    {
        public readonly List<Exception> Error = new();
        public readonly List<T> Next = new();

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            Error.Add(error);
        }

        public void OnNext(T value)
        {
            Next.Add(value);
        }
    }

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
