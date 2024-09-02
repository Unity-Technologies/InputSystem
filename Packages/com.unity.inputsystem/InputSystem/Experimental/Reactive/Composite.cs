using System;

namespace UnityEngine.InputSystem.Experimental
{
    // We could do a 1D composite, this would basically be
    // float x = positiveX - negativeX;
    // float y = positiveY - negativeY;
    
    public struct CompositeProxy<TSource0, TSource1> : IObservableInputNode<float>
        where TSource0 : IObservableInputNode<bool>, IDependencyGraphNode
        where TSource1 : IObservableInputNode<bool>, IDependencyGraphNode
    {
        private sealed class Impl : IObserver<ValueTuple<bool, bool>> // TODO Extract to get tid of TSource
        {
            private ValueTuple<bool, bool> m_Value;
            private readonly ObserverList2<float> m_Observers;
            
            public Impl(Context context, TSource0 source0, TSource1 source1) // TODO Pass list of disposables instead
            {
                // TODO This is one way to implement composite, but it might be better to use same strategy as merge to avoid copying? (But this is simpler)
                var combineLatest = new CombineLatest<bool, bool, TSource0, TSource1>(source0, source1); 
                m_Observers = new ObserverList2<float>(combineLatest.Subscribe(context, this));
            }

            public IDisposable Subscribe(IObserver<float> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<float> observer) =>
                m_Observers.Subscribe(context, observer);

            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);

            public void OnNext(ValueTuple<bool, bool> value) // TODO This would be way easier if we could have a source reference
            {
                if (value == m_Value) 
                    return; // No change
                
                m_Value = value;
                var v = (value.Item1 ? -1.0f : 0.0f) + (value.Item2 ? 1.0f : 0.0f);
                m_Observers.OnNext(v);
            }
        }
        
        private readonly TSource0 m_Source0;
        private readonly TSource1 m_Source1;
        
        public CompositeProxy([InputPort] TSource0 source0, [InputPort] TSource1 source1)
        {
            m_Source0 = source0;
            m_Source1 = source1;
        }

        public IDisposable Subscribe(IObserver<float> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<float>
        {
            return new Impl(context, m_Source0, m_Source1).Subscribe(context, observer);
        }
        
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

    public static partial class Combine
    {
        /// <summary>
        /// Constructs a 1D-axis composite from two buttons.
        /// </summary>
        /// <param name="negative">Boolean indicating negative direction along the axis</param>
        /// <param name="positive">Boolean indicating positive direction along the axis</param>
        /// <typeparam name="TNegative">The negative source observable.</typeparam>
        /// <typeparam name="TPositive">The positive source observable.</typeparam>
        /// <returns>Composite representing a 1D axis.</returns>
        public static CompositeProxy<TNegative, TPositive> Composite<TNegative, TPositive>(TNegative negative, TPositive positive)
            where TNegative : IObservableInputNode<bool>
            where TPositive : IObservableInputNode<bool>
        {
            return new CompositeProxy<TNegative, TPositive>(negative, positive);
        }
        
        // TODO Provide a 2D composite
        
        // TODO Provide a 3D composite
    }
}