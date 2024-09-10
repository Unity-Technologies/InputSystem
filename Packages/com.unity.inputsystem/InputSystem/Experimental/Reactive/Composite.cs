using System;

namespace UnityEngine.InputSystem.Experimental
{
    // We could do a 1D composite, this would basically be
    // float x = positiveX - negativeX;
    // float y = positiveY - negativeY;
    
    public struct CompositeAxisProxy<TSource0, TSource1> : IObservableInputNode<float>
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
        
        public CompositeAxisProxy([InputPort] TSource0 source0, [InputPort] TSource1 source1)
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
    
    public struct CompositeVector2Proxy<TSource0, TSource1, TSource2, TSource3> : IObservableInputNode<Vector2>
        where TSource0 : IObservableInputNode<bool>, IDependencyGraphNode
        where TSource1 : IObservableInputNode<bool>, IDependencyGraphNode
        where TSource2 : IObservableInputNode<bool>, IDependencyGraphNode
        where TSource3 : IObservableInputNode<bool>, IDependencyGraphNode
    {
        private sealed class Impl : IObserver<ValueTuple<bool, bool, bool, bool>> // TODO Extract to get tid of TSource
        {
            private ValueTuple<bool, bool, bool, bool> m_Value;
            private readonly ObserverList2<Vector2> m_Observers;
            
            public Impl(Context context, TSource0 source0, TSource1 source1, TSource2 source2, TSource3 source3) // TODO Pass list of disposables instead
            {
                // TODO This is one way to implement composite, but it might be better to use same strategy as merge to avoid copying? (But this is simpler)
                var combineLatest = new CombineLatest<bool, bool, bool, bool, TSource0, TSource1, TSource2, TSource3>(source0, source1, source2, source3); 
                m_Observers = new ObserverList2<Vector2>(combineLatest.Subscribe(context, this));
            }

            public IDisposable Subscribe(IObserver<Vector2> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<Vector2> observer) =>
                m_Observers.Subscribe(context, observer);

            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);

            public void OnNext(ValueTuple<bool, bool, bool, bool> value) // TODO This would be way easier if we could have a source reference
            {
                if (value == m_Value) 
                    return; // No change
                
                m_Value = value;
                var x = (value.Item1 ? -1.0f : 0.0f) + (value.Item2 ? 1.0f : 0.0f);
                var y = (value.Item3 ? -1.0f : 0.0f) + (value.Item4 ? 1.0f : 0.0f);
                m_Observers.OnNext(new Vector2(x, y));
            }
        }
        
        private readonly TSource0 m_Source0;
        private readonly TSource1 m_Source1;
        private readonly TSource2 m_Source2;
        private readonly TSource3 m_Source3;
        
        public CompositeVector2Proxy([InputPort] TSource0 source0, [InputPort] TSource1 source1, [InputPort] TSource2 source2, [InputPort] TSource3 source3)
        {
            m_Source0 = source0;
            m_Source1 = source1;
            m_Source2 = source2;
            m_Source3 = source3;
        }

        public IDisposable Subscribe(IObserver<Vector2> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<Vector2>
        {
            return new Impl(context, m_Source0, m_Source1, m_Source2, m_Source3).Subscribe(context, observer);
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
        public static CompositeAxisProxy<TNegative, TPositive> Composite<TNegative, TPositive>(
            TNegative negative, TPositive positive)
            where TNegative : IObservableInputNode<bool>
            where TPositive : IObservableInputNode<bool>
        {
            return new CompositeAxisProxy<TNegative, TPositive>(negative, positive);
        }
        
        public static CompositeAxisProxy<TNegative, TPositive> Composite<TNegative, TPositive>(
            CompositeExpression<TNegative, TPositive> expression)
            where TNegative : IObservableInputNode<bool>
            where TPositive : IObservableInputNode<bool>
        {
            return new CompositeAxisProxy<TNegative, TPositive>(expression.source0, expression.source1);
        }
        
        public static CompositeVector2Proxy<TNegativeX, TPositiveX, TNegativeY, TPositiveY> 
            Composite<TNegativeX, TPositiveX, TNegativeY, TPositiveY>(
                TNegativeX negativeX, TPositiveX positiveX, TNegativeY negativeY, TPositiveY positiveY)
            where TNegativeX : IObservableInputNode<bool>
            where TPositiveX : IObservableInputNode<bool>
            where TNegativeY : IObservableInputNode<bool>
            where TPositiveY : IObservableInputNode<bool>
        {
            return new CompositeVector2Proxy<TNegativeX, TPositiveX, TNegativeY, TPositiveY>(
                negativeX, positiveX, negativeY, positiveY);
        }
        
        // TODO Provide a 3D composite
    }
    
    /// <summary>
    /// A composite expression of arbitrary types.
    /// </summary>
    /// <typeparam name="TSource0">The first observable source type.</typeparam>
    /// <typeparam name="TSource1">The second observable source type.</typeparam>
    public readonly struct CompositeExpression<TSource0, TSource1>
    {
        public CompositeExpression(in TSource0 source0, in TSource1 source1)
        {
            this.source0 = source0;
            this.source1 = source1;
        }

        public TSource0 source0 { get; }

        public TSource1 source1 { get; }
    }
    
    public struct CompositeExpression<TSource0, TSource1, TSource2>
    {
        public CompositeExpression(TSource0 source0, TSource1 source1, TSource2 source2)
        {
            this.source0 = source0;
            this.source1 = source1;
            this.source2 = source2;
        }
        
        public TSource0 source0 { get; }

        public TSource1 source1 { get; }

        public TSource2 source2 { get; }
    }
    
    public struct CompositeExpression<TSource0, TSource1, TSource2, TSource3>
    {
        public CompositeExpression(TSource0 source0, TSource1 source1, TSource2 source2, TSource3 source3)
        {
            this.source0 = source0;
            this.source1 = source1;
            this.source2 = source2;
            this.source3 = source3;
        }
        
        public TSource0 source0 { get; }

        public TSource1 source1 { get; }

        public TSource2 source2 { get; }

        public TSource3 source3 { get; }
    }
    
    public static class CompositeExtensions
    {
        public static CompositeExpression<TSource0, TSource1> Combine<TSource0, TSource1>(
            this TSource0 source0, TSource1 source1)
        {
            return new CompositeExpression<TSource0, TSource1>(source0, source1);
        }
        
        public static CompositeExpression<TSource0, TSource1, TSource2> Combine<TSource0, TSource1, TSource2>(
            this CompositeExpression<TSource0, TSource1> expression, TSource2 source2)
        {
            return new CompositeExpression<TSource0, TSource1, TSource2>(expression.source0, expression.source1, source2);
        }
        
        public static CompositeExpression<TSource0, TSource1, TSource2, TSource3> Combine<TSource0, TSource1, TSource2, TSource3>(
            this CompositeExpression<TSource0, TSource1, TSource2> expression, TSource3 source)
        {
            return new CompositeExpression<TSource0, TSource1, TSource2, TSource3>(
                expression.source0, expression.source1, expression.source2, source);
        }
    }

    public static class Vector2CompositeExtensions
    {
        public static CompositeVector2Proxy<TNegativeX, TPositiveX, TNegativeY, TPositiveY> 
            ToComposite<TNegativeX, TPositiveX, TNegativeY, TPositiveY>(this CompositeExpression<TNegativeX, TPositiveX, TNegativeY, TPositiveY> expression)
            where TNegativeX : IObservableInputNode<bool>
            where TPositiveX : IObservableInputNode<bool>
            where TNegativeY : IObservableInputNode<bool>
            where TPositiveY : IObservableInputNode<bool>
        {
            return new CompositeVector2Proxy<TNegativeX, TPositiveX, TNegativeY, TPositiveY>(expression.source0, expression.source1, expression.source2, expression.source3);
        }
    }
}