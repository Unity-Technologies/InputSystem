using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO This is basically AND, redesign as such?
    public struct Chord<TSource> : IObservableInput<bool>, IDependencyGraphNode
        where TSource : IObservableInput<bool>, IDependencyGraphNode
    {
        private sealed class Impl : IObserver<ValueTuple<bool, bool>>
        {
            private bool m_Value;
            private readonly ObserverList2<bool> m_Observers;
            
            public Impl(Context context, TSource source0, TSource source1)
            {
                var combineLatest = new CombineLatest<bool, bool, TSource, TSource>(source0, source1);
                m_Observers = new ObserverList2<bool>(combineLatest.Subscribe(context, this));
            }

            public IDisposable Subscribe(IObserver<bool> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<bool> observer) =>
                m_Observers.Subscribe(context, observer);

            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);

            public void OnNext(ValueTuple<bool, bool> value) // TODO This would be way easier if we could have a source reference
            {
                var newValue = value is { Item1: true, Item2: true };
                if (newValue != m_Value)
                {
                    m_Value = newValue;
                    m_Observers.OnNext(newValue);
                }
            }
        }
        
        private readonly TSource m_Source0;
        private readonly TSource m_Source1;
        private Impl m_Impl;
        
        internal Chord([InputPort] TSource source0, [InputPort] TSource source1)
        {
            m_Source0 = source0;
            m_Source1 = source1;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<bool> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<bool> observer) =>
            (m_Impl ??= new Impl(context, m_Source0, m_Source1)).Subscribe(context, observer);
        
        // TODO Reader end-point
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "Chord";
        public int childCount => 1;

        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Source0;
                case 1: return m_Source1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
    
    public static class ChordExtensionMethods
    {
        public static Chord<TSource> Chord<TSource>(this TSource source1, TSource source2)
            where TSource : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Chord<TSource>(source1, source2);
        }
    }
}