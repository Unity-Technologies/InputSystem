using System;
using Unity.IO.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    /*public struct DeferredProxy<TSource, T> : IObservableInputNode<T> 
        where T : struct
    {
        private TSource m_Source;
        private int m_Priority;
        
        public DeferredProxy(TSource source, int priority)
        {
            m_Source = source;
            m_Priority = priority;
        }

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) where TObserver : IObserver<T>
        {
            throw new NotImplementedException();
        }
    }

    public static class DeferExtensions
    {
        public static DeferredProxy<TSource, T> Defer<TSource, T>(this TSource source, int priority)
            where TSource : IObservableInputNode<T> 
            where T : struct
        {
            return new DeferredProxy<TSource, T>(source, priority);
        }
    }*/
}