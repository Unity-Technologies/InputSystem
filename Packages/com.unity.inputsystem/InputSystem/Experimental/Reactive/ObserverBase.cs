using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental.Internal;

namespace UnityEngine.InputSystem.Experimental
{
    public class ObserverCommon
    {
        public const int DefaultPriority = 0;
        
        private readonly int m_Priority;

        protected ObserverCommon(int priority = DefaultPriority)
        {
            m_Priority = priority;
        }
    }
    
    public interface IForwardOnNext<T>
    {
        public void ForwardOnNext(T value);
    }
    
    // Should priority basically create a gate that stores output and defer 
    
    public class ObserverBase<TOut> : ObserverCommon, IForwardOnNext<TOut> 
        where TOut : unmanaged
    {
        private SubscriptionSettings m_Settings;
        private IObserver<TOut>[] m_Observers;
        private int m_ObserverCount;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="priority">The associated output priority.</param>
        protected ObserverBase(int priority = DefaultPriority)
            : base(priority)
        { }

        /// <summary>
        /// The associated context.
        /// </summary>
        protected Context context { get; set; }
        
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
        /// <param name="observer">The observer to be removed.</param>
        /// <returns>true if the observer was the last observer of this node, else false</returns>
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
        public void ForwardOnNext(TOut value)
        {
            // TODO Shortcuts:
            // As long as we process a single event at a time...
            // Only that event may trickle down and trigger reactions in the dependency chain...
            // If we would call chains with higher priority first, e.g. Ctrl+C when C is pressed, this would allow
            // "consuming" that C event by aborting processing if Ctrl+C fires.
            //
            // Another way of doing it would be to allow all events to fire, and then if there are multiple tentative events with the same eventId only allow that/those with highest priority through.
            // This wouldn't require any particular order of execution which might be desirable. If no priority is set this would not be done.
            // A problem is still how to represents a priority on dependency chain, but if do not cache/reuse nodes this becomes simpler, or we exclude priority chains from caching/reuse.
            // Revisited: Maybe not, the priority is stored at subscription. Hence it sits with the end node. Lets try this.
            // Future: This could also work will async processing of individual chains since it would require merge/sort on eventId which would be derived.
            //
            // Keyboard ----> C -----> Shortcut ----> Pressed
            //                  -----> Pressed
            //
            // One particular problem with this is that it becomes most relevant in end-node, but we cannot really say what is an end node.
            //
            // A simpler fix for this particular issue would be to identify that C is part of 2 flows. (Has 2 observers)
            // If C is part of 2 flows it may result in OnNext propagation depending on state machines of downstream nodes. 
            // However, this would require reused nodes.
            if (m_Settings.eventGroupId != 0)
            {
                //context.Defer(this, Priority,);
                // TODO We should move TOut and reference back to node into context, can be do this better via node interface
                //context.Defer(() => value, m_Settings.eventGroupPriority);
            }
            for (var i = 0; i < m_ObserverCount; ++i)
                m_Observers[i].OnNext(value);
        }

        private void DoForwardOnNext(TOut value)
        {
            for (var i = 0; i < m_ObserverCount; ++i)
                m_Observers[i].OnNext(value);
        }

        internal unsafe void DeferredForwardOnNext(void* data)
        {
            // TODO Assert correct alignment of data
            DoForwardOnNext(*(TOut*)data);
        }
        
        // TODO These should really be in derived but not sure we should support proper RX
        public void OnCompleted() => ForwardOnCompleted();
        public void OnError(Exception error) => ForwardOnError(error);
    }
}