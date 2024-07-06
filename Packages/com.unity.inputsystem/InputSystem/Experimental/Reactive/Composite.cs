using System;

namespace UnityEngine.InputSystem.Experimental
{
    // We could do a 1D composite, this would basically be
    // float x = positiveX - negativeX;
    // float y = positiveY - negativeY;
    
    // TODO This is basically AND, redesign as such?
    public struct Composite<TSource> : IObservableInput<bool>, IDependencyGraphNode
        where TSource : IObservableInput<bool>, IDependencyGraphNode
    {
        private sealed class Impl : IObserver<ValueTuple<bool, bool>>
        {
            private bool m_Value;
            private readonly ObserverList2<bool> m_Observers;
            
            public Impl(Context context, TSource source0, TSource source1)
            {
                // TODO This is one way to implement composite, but it might be better to use same strategy as merge to avoid copying? (But this is simpler)
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
                if (newValue == m_Value) 
                    return; // No change
                
                m_Value = newValue;
                m_Observers.OnNext(newValue);
            }
        }
        
        private readonly TSource m_Source0;
        private readonly TSource m_Source1;
        private Impl m_Impl;
        
        public Composite([InputPort] TSource source0, [InputPort] TSource source1)
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
        
        public string displayName => "Composite";
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
}