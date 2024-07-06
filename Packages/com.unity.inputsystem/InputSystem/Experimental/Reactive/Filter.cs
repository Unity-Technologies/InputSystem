using System;

namespace UnityEngine.InputSystem.Experimental
{
    public struct Filter<T, TSource> : IObservableInput<T>
        where T : struct
        where TSource : IObservableInput<T>
    {
        private class Impl : IObserver<T>
        {
            private readonly ObserverList2<T> m_Observers;
            private readonly Predicate<T> m_Predicate;

            public Impl(Context context, TSource source, Predicate<T> predicate)
            {
                m_Observers = new ObserverList2<T>(source.Subscribe(context, this));
                m_Predicate = predicate;
            }
            
            public IDisposable Subscribe(IObserver<T> observer) => 
               Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<T> observer) =>
                m_Observers.Subscribe(context, observer);

            
            private void Process(T value)
            {
                if (m_Predicate(value))
                    m_Observers.OnNext(value);
            }
            
            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);
            public void OnNext(T value) => Process(value);
        }
        
        
        private readonly TSource m_Source;
        private Impl m_Impl;
        private readonly Predicate<T> m_Predicate;
        
        public Filter(TSource source, Predicate<T> predicate)
        {
            m_Source = source;
            m_Predicate = predicate;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<T> observer) =>
            (m_Impl ??= new Impl(context, m_Source, m_Predicate)).Subscribe(context, observer);
        
        public bool Equals(IDependencyGraphNode other)
        {
            throw new NotImplementedException();
        }
        
        public string displayName => "Filter";
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>
    /// Allows applying Filter processing to an observable input source.
    /// </summary>
    public static class FilterExtensionMethods
    {
        public static Filter<T, IObservableInput<T>> Filter<T>(this IObservableInput<T> source, Predicate<T> predicate) 
            where T : struct 
        {
            return new Filter<T, IObservableInput<T>>(source, predicate);
        }
        
        public static Filter<T, TSource> Filter<T, TSource>(this TSource source, Predicate<T> predicate) 
            where T : struct 
            where TSource : IObservableInput<T>
        {
            return new Filter<T, TSource>(source, predicate);
        }
    }
}