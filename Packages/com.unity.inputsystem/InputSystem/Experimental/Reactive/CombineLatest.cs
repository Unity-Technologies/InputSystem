using System;

namespace UnityEngine.InputSystem.Experimental
{

    public struct CombineLatest<T0, T1, TSource0, TSource1> : IObservableInput<ValueTuple<T0, T1>>, IDependencyGraphNode
        where TSource0 : IObservableInput<T0>, IDependencyGraphNode
        where TSource1 : IObservableInput<T1>, IDependencyGraphNode
        where T0 : struct
        where T1 : struct
    {
        private sealed class Impl //: IObserver<ValueTuple<T0, T1>>
        {
            private bool m_PreviousValue;
            private readonly ObserverList2<ValueTuple<T0, T1>> m_Observers;
            private readonly FirstObserver m_FirstObserver;
            private readonly SecondObserver m_SecondObserver;
            private ValueTuple<T0, T1> m_Value;

            private sealed class FirstObserver : IObserver<T0>
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
                    // TODO Should not emit if same atomic source until all reporter
                    m_Parent.m_Value = new ValueTuple<T0, T1>(value, m_Parent.m_Value.Item2);
                    m_Parent.ForwardOnNext();
                }
            }

            private sealed class SecondObserver : IObserver<T1>
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
                    // TODO Should not emit if same atomic source until all reporter
                    m_Parent.m_Value = new ValueTuple<T0, T1>(m_Parent.m_Value.Item1, value);
                    m_Parent.ForwardOnNext();
                }
            }
        
            public Impl(Context context, TSource0 source0, TSource1 source1)
            {
                m_FirstObserver = new FirstObserver(this);
                m_SecondObserver = new SecondObserver(this);
                
                m_Observers = new ObserverList2<ValueTuple<T0, T1>>( 
                    source0.Subscribe(context, m_FirstObserver),
                    source1.Subscribe(context, m_SecondObserver));
            }

            public IDisposable Subscribe(IObserver<ValueTuple<T0, T1>> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

            public IDisposable Subscribe(Context context, IObserver<ValueTuple<T0, T1>> observer) =>
                m_Observers.Subscribe(context, observer);

            /*public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);

            public void OnNext(ValueTuple<T0, T1> value)
            {
                m_Observers.OnNext(m_Value);
            }*/

            private void ForwardOnNext()
            {
                m_Observers.OnNext(m_Value);
            }
        }
        
        private readonly TSource0 m_Source0;
        private readonly TSource1 m_Source1;
        private Impl m_Impl;
        
        internal CombineLatest(TSource0 source0, TSource1 source1)
        {
            m_Source0 = source0;
            m_Source1 = source1;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<ValueTuple<T0, T1>> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<ValueTuple<T0, T1>> observer) =>
            (m_Impl ??= new Impl(context, m_Source0, m_Source1)).Subscribe(context, observer);
        
        // TODO Reader end-point
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);

        public int nodeId => 0; // TODO Remove
        public string displayName => "Press";
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

    public static class Combine
    {
        public static CombineLatest<T0, T1, IObservableInput<T0>, IObservableInput<T1>> CombineLatest<T0, T1>(
            IObservableInput<T0> source0, IObservableInput<T1> source1)
            where T0 : struct
            where T1 : struct
        {
            // TODO Possibility to check here for concrete instances but keep simple syntax, overload with exact
            
            return new CombineLatest<T0, T1, IObservableInput<T0>, IObservableInput<T1>>(source0, source1);
        }
        
        public static Chord<TSourceOther> Chord<TSourceOther>(TSourceOther source1, TSourceOther source2)
            where TSourceOther : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Chord<TSourceOther>(source1, source2);
        }
    }
    
    /// <summary>
    /// Allows applying Press interaction evaluation on an observable source.
    /// </summary>
    public static class CombineExtensionMethods
    {
        public static CombineLatest<T0, T1, TSource0, TSource1> CombineLatest<T0, T1, TSource0, TSource1>(
            this TSource0 source0, TSource1 source1)
            where TSource0 : IObservableInput<T0>, IDependencyGraphNode
            where TSource1 : IObservableInput<T1>, IDependencyGraphNode
            where T0 : struct
            where T1 : struct
        {
            return new CombineLatest<T0, T1, TSource0, TSource1>(source0, source1);
        }
    }    
    
}