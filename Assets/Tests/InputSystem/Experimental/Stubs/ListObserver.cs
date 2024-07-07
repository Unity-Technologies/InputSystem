using System;
using System.Collections.Generic;

namespace Tests.InputSystem
{
    internal sealed class ListObserver<T> : IObserver<T>
    {
        public bool Completed = false;
        public readonly List<Exception> Error = new();
        public readonly List<T> Next = new();

        public void OnCompleted()
        {
            Completed = true;
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
}