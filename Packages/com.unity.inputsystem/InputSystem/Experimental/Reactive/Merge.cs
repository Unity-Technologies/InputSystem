using System;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider optimizing for avoiding boxing for small numbers of sources
    // TODO Problems seens with serializing merge
    
    /// <summary>
    /// Multiplexes two streams of type <typeparamref name="T"/> by interleaving items from each of the
    /// streams in the order they where emitted..
    /// </summary>
    /// <typeparam name="T">The type parameter.</typeparam>
    /// <typeparam name="TSource0">The first source type.</typeparam>
    /// <typeparam name="TSource1">The second source type.</typeparam>
    [Serializable]
    public struct Merge<T, TSource0, TSource1> : IObservableInputNode<T>, IDependencyGraphNode
        where TSource0 : IObservableInputNode<T>, IDependencyGraphNode
        where TSource1 : IObservableInputNode<T>, IDependencyGraphNode
        where T : struct
    {
        private sealed class Impl : IForwardReceiver<T>
        {
            private readonly ObserverList2<T> m_Observers;
        
            public Impl(Context context, TSource0 source0, TSource1 source1) 
            {
                var firstObserver = new ForwardingObserver<T, Impl>(this);
                var secondObserver = new ForwardingObserver<T, Impl>(this);
                
                m_Observers = new ObserverList2<T>( 
                    source0.Subscribe(context, firstObserver),
                    source1.Subscribe(context, secondObserver));
            }

            public IDisposable Subscribe(IObserver<T> observer) => 
                Subscribe(Context.instance, observer);

            public IDisposable Subscribe(Context context, IObserver<T> observer) =>
                m_Observers.Subscribe(context, observer);
            
            public void ForwardOnCompleted()
            {
                throw new NotImplementedException();
            }

            public void ForwardOnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public void ForwardOnNext(T value)
            {
                m_Observers.OnNext(value);
            }
        }
        
        [SerializeField] private TSource0 source0; // TODO This is problematic if not concrete type, that would require SerializeReference. Source generation could guarantee this but currently unreliable
        [SerializeField] private TSource1 source1;
        
        public Merge(TSource0 source0, TSource1 source1)
        {
            // TODO Check source0, check source1 when using constructor
            
            this.source0 = source0;
            this.source1 = source1;
        }

        public IDisposable Subscribe(IObserver<T> observer) => Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<T>
        {
            return new Impl(context, source0, source1).Subscribe(context, observer);
        }
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "Merge";
        public int childCount => 2;

        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return source0;
                case 1: return source1;
                default: throw new ArgumentOutOfRangeException(nameof(index)); 
            }
        }
    }
    
    [Serializable]
    public struct Merge<T> : IObservableInputNode<T>, IDependencyGraphNode
        where T : struct
    {
        private sealed class Impl : IForwardReceiver<T>
        {
            private readonly ObserverList2<T> m_Observers;
        
            public Impl(Context context, IObservableInput<T> source0, IObservableInput<T> source1) 
            {
                var firstObserver = new ForwardingObserver<T, Impl>(this);
                var secondObserver = new ForwardingObserver<T, Impl>(this);

                // TODO When we have multiple subscriptions, we might want to do try-catch and warn about subscriptions that could be setup?!
                IDisposable subscription0 = source0.Subscribe(context, firstObserver);
                IDisposable subscription1 = source1.Subscribe(context, secondObserver);
                
                m_Observers = new ObserverList2<T>(subscription0, subscription1);
            }

            public IDisposable Subscribe(IObserver<T> observer) => 
                Subscribe(Context.instance, observer);

            public IDisposable Subscribe(Context context, IObserver<T> observer) =>
                m_Observers.Subscribe(context, observer);
            
            public void ForwardOnCompleted()
            {
                throw new NotImplementedException();
            }

            public void ForwardOnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public void ForwardOnNext(T value)
            {
                m_Observers.OnNext(value);
            }
        }
        
        [SerializeReference] private IObservableInputNode<T> source0; // TODO This is problematic if not concrete type, that would require SerializeReference. Source generation could guarantee this but currently unreliable
        [SerializeReference] private IObservableInputNode<T> source1;
        
        public Merge(IObservableInputNode<T> source0, IObservableInputNode<T> source1)
        {
            this.source0 = source0;
            this.source1 = source1;
        }

        public IDisposable Subscribe(IObserver<T> observer) => Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<T>
        {
            return new Impl(context, source0, source1).Subscribe(context, observer);
        }
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "Merge";
        public int childCount => 2;

        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return source0;
                case 1: return source1;
                default: throw new ArgumentOutOfRangeException(nameof(index)); 
            }
        }
    }

    public static partial class Combine
    {
        public static Merge<T, TSource0, TSource1> Merge<T, TSource0, TSource1>(
            TSource0 source0, TSource1 source1)
            where T : struct 
            where TSource0 : IObservableInputNode<T> 
            where TSource1 : IObservableInputNode<T>
        {
            return new Merge<T, TSource0, TSource1>(source0, source1);
        }
        
        // TODO Consider if we should generate merge extensions similar to this for supported types to avoid boxing. This would be possible within package but would be problematic for extensions? Use extensions within project to verify.
        public static Merge<bool, TSource0, TSource1> Merge<TSource0, TSource1>(
            TSource0 source0, TSource1 source1)
            where TSource0 : IObservableInputNode<bool> 
            where TSource1 : IObservableInputNode<bool>
        {
            return new Merge<bool, TSource0, TSource1>(source0, source1);
        }
        
        /// <summary>
        /// Constructs a type-erased merge (interleaved multiplexing) of two inputs.
        /// </summary>
        /// <param name="source0">The first input.</param>
        /// <param name="source1">The second input.</param>
        /// <typeparam name="T">The value type.</typeparam>
        /// <returns><see cref="Merge{T}"/> instance, never null.</returns>
        public static Merge<T> Merge<T>(IObservableInputNode<T> source0, IObservableInputNode<T> source1)
            where T : struct 
        {
            return new Merge<T>(source0, source1);
        }
    }
}