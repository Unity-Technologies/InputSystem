using System;

namespace UnityEngine.InputSystem.Experimental
{
    public class BindableOutput<T> : IObserver<T>, IDisposable where T : struct
    {
        public BindableOutput(OutputBindingTarget<T> binding)
        {
        }

        public void Dispose()
        {
        }

        public void Offer(T item)
        {
            // TODO Apply to output bindings
        }

        public void Offer(ref T item)
        {
            // TODO Apply to output bindings
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            throw new NotImplementedException();
        }
    }
}
