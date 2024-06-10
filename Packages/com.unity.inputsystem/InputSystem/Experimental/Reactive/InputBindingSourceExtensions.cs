using System;

namespace UnityEngine.InputSystem.Experimental
{
    public static class InputBindingSourceExtensions
    {
        // Internal helper class that wraps an Action as an IObserver
        private sealed class ObserverDelegateWrapper<T> : IObserver<T>
        {
            private readonly Action<T> m_Action;
            public ObserverDelegateWrapper(Action<T> action) => m_Action = action;
            public void OnCompleted() { } // ignored
            public void OnError(Exception error) => Debug.LogException(error);
            public void OnNext(T value) => m_Action(value);
        }
        
        public static IDisposable Subscribe<T, TSource>(this TSource observable, Action<T> action) 
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return observable.Subscribe(new ObserverDelegateWrapper<T>(action));
        }

        /*public static IDisposable Merge<T, TSource1, TSource2>(this TSource1 source1, TSource2 source2)
            where T : struct
            where TSource1 : IInputBindingSource<T>
            where TSource2 : IInputBindingSource<T>
        {
            return new MergeOp<T>(source1, source2);
        }*/
    }
}