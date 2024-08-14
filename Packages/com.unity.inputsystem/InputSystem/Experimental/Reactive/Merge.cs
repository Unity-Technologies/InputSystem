using System;

namespace UnityEngine.InputSystem.Experimental
{
    public struct Merge<T, TSource> : IObservableInputNode<T>, IDependencyGraphNode
        where TSource : IObservableInputNode<T>, IDependencyGraphNode
        where T : struct
    {
        private sealed class Impl
        {
            private readonly ObserverList2<T> m_Observers;
            
            private sealed class FirstObserver : IObserver<T>
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

                public void OnNext(T value)
                {
                    m_Parent.ForwardOnNext(value);
                }
            }

            private sealed class SecondObserver : IObserver<T>
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

                public void OnNext(T value)
                {
                    m_Parent.ForwardOnNext(value);
                }
            }
        
            public Impl(Context context, TSource source0, TSource source1)
            {
                var firstObserver = new FirstObserver(this);
                var secondObserver = new SecondObserver(this);
                
                m_Observers = new ObserverList2<T>( 
                    source0.Subscribe(context, firstObserver),
                    source1.Subscribe(context, secondObserver));
            }

            public IDisposable Subscribe(IObserver<T> observer) => 
                Subscribe(Context.instance, observer);

            public IDisposable Subscribe(Context context, IObserver<T> observer) =>
                m_Observers.Subscribe(context, observer);

            private void ForwardOnNext(T value) => m_Observers.OnNext(value);
        }
        
        private readonly TSource m_Source0;
        private readonly TSource m_Source1;
        private Impl m_Impl;
        
        public Merge(TSource source0, TSource source1)
        {
            m_Source0 = source0;
            m_Source1 = source1;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<T> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<T> =>
            (m_Impl ??= new Impl(context, m_Source0, m_Source1)).Subscribe(context, observer);
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "Merge";
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
        public static Merge<T, IObservableInputNode<T>> Merge<T>(
            IObservableInputNode<T> source0, IObservableInputNode<T> source1)
            where T : struct
        {
            return new Merge<T, IObservableInputNode<T>>(source0, source1);
        }
    }
}