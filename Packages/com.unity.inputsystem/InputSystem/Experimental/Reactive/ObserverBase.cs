using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental.Internal;

namespace UnityEngine.InputSystem.Experimental
{
    public class ObserverCommon
    {
        protected void DisposeAndReset(ref IDisposable disposable)
        {
            disposable.Dispose();
            disposable = null;
        }
    }
    
    // Should priority basically create a gate that stores output and defer 
    
    public class ObserverBase<TOut> : ObserverCommon where TOut : struct
    {
        private IObserver<TOut>[] m_Observers;
        private int m_ObserverCount;

        /*public struct Proxy<TNode, TIn, TSource>
            where TSource : IObservableInputNode<TIn> 
            where TIn : struct
            where TNode : ObserverBase<TOut>, IObserver<TIn>, IUnsubscribe<TOut>, new()
        {
            private TSource m_Source;
            
            public Proxy([InputPort] TSource source)
            {
                m_Source = source;
            }
            
            public IDisposable Subscribe([NotNull] IObserver<TOut> observer) => 
                Subscribe(Context.instance, observer);
            
            public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
                where TObserver : IObserver<TOut>
            {
                // TODO Implement node sharing (multi-cast)

                // Construct node instance and register underlying subscriptions.
                var impl = ObjectPool<TNode>.shared.Rent();
                impl.Initialize(m_Source.Subscribe(context, impl) ); 

                // Register observer with node and return subscription
                impl.AddObserver(observer);
                return new Subscription<TOut>(impl, observer);
            }
        }*/
        
        /// <summary>
        /// Adds an observer to the list of observers of this node.
        /// </summary>
        /// <param name="observer">The observer to be added.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="observer"/> is <c>null</c>.</exception>
        public void AddObserver([NotNull] IObserver<TOut> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));
            ArrayPool<IObserver<TOut>>.Shared.Append(ref m_Observers, observer, ref m_ObserverCount);
        }

        /// <summary>
        /// Removes an observer from the list of observers of this node.
        /// </summary>
        /// <param name="observer"></param>
        /// <returns></returns>
        public bool RemoveObserver([NotNull] IObserver<TOut> observer)
        {
            ArrayPool<IObserver<TOut>>.Shared.Remove(ref m_Observers, observer, ref m_ObserverCount);
            return m_ObserverCount == 0;
        }
        
        /// <summary>
        /// Forwards a completion signal to all observers.
        /// </summary>
        protected void ForwardOnCompleted() 
        {
            for (var i = 0; i < m_ObserverCount; ++i)
                m_Observers[i].OnCompleted();
        }

        /// <summary>
        /// Forwards an error signal to all observers.
        /// </summary>
        /// <param name="error"></param>
        protected void ForwardOnError(Exception error) 
        { 
            for (var i = 0; i < m_ObserverCount; ++i)
                m_Observers[i].OnError(error);
        }
        
        /// <summary>
        /// Forwards a value to all observers.
        /// </summary>
        /// <param name="value"></param>
        protected void ForwardOnNext(TOut value)
        {
            for (var i = 0; i < m_ObserverCount; ++i)
                m_Observers[i].OnNext(value);
        }
        
        // TODO These should really be in derived but not sure we should support proper RX
        public void OnCompleted() => ForwardOnCompleted();
        public void OnError(Exception error) => ForwardOnError(error);
    }
}