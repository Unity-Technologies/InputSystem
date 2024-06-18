using System;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IInputObservable<T>
    {
        public void OnNext(ref T value);
    }

    public interface IInputObserver<T>
    {
        public Subscription Subscribe(Context context, IInputObserver<T> observer);
    }

    public static class InputObserverExtensions
    {
        public static Subscription Subscribe<T, TObserver>(this TObserver observer)
            where TObserver : IInputObserver<T>
        {
            return observer.Subscribe(Context.instance, observer);
        }
    }

    public struct Subscription : IDisposable
    {
        private readonly int m_ContextHandle;   // Handle that may be used to find Context instance
        private readonly int m_Handle;          // Handle that may be used to find Subscription instance

        public void Dispose()
        {
            // Find associated i
            var context = Context.GetContext(m_ContextHandle);
            if (context == null)
                throw new Exception("Unable to find associated context");
            
            // Unsubscribe
            // TODO context.Unsubscribe(m_Handle);
        }
    }
}