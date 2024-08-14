using System;

// TODO Evaluate if we could also make Impl a struct. Some Impl requires to track state and hence we should support
//      allocating such state. If state is native memory we can avoid GC. However, we need to also keep a list
//      of managed objects mapping to IDisposable subscriptions and IObservable<T> representing callbacks in chain.

namespace UnityEngine.InputSystem.Experimental
{
    // TODO CombineLatest Should not emit if same atomic source until all has reported for timestep t
    public struct CombineLatest<T0, T1, TSource0, TSource1> : IObservableInputNode<ValueTuple<T0, T1>>, IDependencyGraphNode
        where TSource0 : IObservableInputNode<T0>, IDependencyGraphNode
        where TSource1 : IObservableInputNode<T1>, IDependencyGraphNode
        where T0 : struct
        where T1 : struct
    {
        private sealed class Impl 
        {
            private readonly ObserverList2<ValueTuple<T0, T1>> m_Observers;
            private ValueTuple<T0, T1> m_Value;

            private readonly struct FirstObserver : IObserver<T0> // TODO Should we use struct or class?!
            {
                private readonly Impl m_Parent;
                
                public FirstObserver(Impl parent)
                {
                    m_Parent = parent;
                }
                
                public void OnCompleted()
                {
                    throw new NotImplementedException();
                }

                public void OnError(Exception error)
                {
                    throw new NotImplementedException();
                }

                public void OnNext(T0 value)
                {
                    m_Parent.m_Value.Item1 = value;
                    m_Parent.ForwardOnNext();
                }
            }

            private readonly struct SecondObserver : IObserver<T1>
            {
                private readonly Impl m_Parent;
                
                public SecondObserver(Impl parent)
                {
                    m_Parent = parent;
                }
                
                public void OnCompleted()
                {
                    throw new NotImplementedException();
                }

                public void OnError(Exception error)
                {
                    throw new NotImplementedException();
                }

                public void OnNext(T1 value)
                {
                    m_Parent.m_Value.Item2 = value;
                    m_Parent.ForwardOnNext();
                }
            }
        
            public Impl(Context context, TSource0 source0, TSource1 source1)
            {
                var firstObserver = new FirstObserver(this);
                var secondObserver = new SecondObserver(this);
                
                // Note that first and second observers are copied and hence should not contain state
                m_Observers = new ObserverList2<ValueTuple<T0, T1>>( 
                    source0.Subscribe(context, firstObserver),
                    source1.Subscribe(context, secondObserver));
            }

            public IDisposable Subscribe(IObserver<ValueTuple<T0, T1>> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<ValueTuple<T0, T1>> observer) =>
                m_Observers.Subscribe(context, observer);
            
            private void ForwardOnNext() => m_Observers.OnNext(m_Value);
        }
        
        private readonly TSource0 m_Source0;
        private readonly TSource1 m_Source1;
        private Impl m_Impl;
        
        public CombineLatest(TSource0 source0, TSource1 source1)
        {
            m_Source0 = source0;
            m_Source1 = source1;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<ValueTuple<T0, T1>> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<ValueTuple<T0, T1>>
        {
            return (m_Impl ??= new Impl(context, m_Source0, m_Source1)).Subscribe(context, observer);
        }
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "CombineLatest";
        public int childCount => 2;

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
        // TODO See if there is some trick we can utilize to keep decent syntax but not type-erase sources
        public static CombineLatest<T0, T1, IObservableInputNode<T0>, IObservableInputNode<T1>> Latest<T0, T1>(
            IObservableInputNode<T0> source0, IObservableInputNode<T1> source1)
            where T0 : struct
            where T1 : struct
        {
            return new CombineLatest<T0, T1, IObservableInputNode<T0>, IObservableInputNode<T1>>(source0, source1);
        }
        
        public static CombineLatest<T0, T1, TSource0, TSource1> Latest<T0, T1, TSource0, TSource1>(
            this TSource0 source0, TSource1 source1)
            where TSource0 : IObservableInputNode<T0>, IDependencyGraphNode
            where TSource1 : IObservableInputNode<T1>, IDependencyGraphNode
            where T0 : struct
            where T1 : struct
        {
            return new CombineLatest<T0, T1, TSource0, TSource1>(source0, source1);
        }
    }
}