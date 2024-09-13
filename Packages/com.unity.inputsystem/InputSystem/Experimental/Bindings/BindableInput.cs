using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    internal sealed class AggregateSubscription : IDisposable
    {
        private IDisposable[] m_Subscriptions;
            
        public AggregateSubscription(int size)
        {
            if (size > 0)
                m_Subscriptions = new IDisposable[size];
        }
            
        public IDisposable this[int key]
        {
            get => m_Subscriptions[key];
            set => m_Subscriptions[key] = value;
        }

        public void Dispose()
        {
            if (m_Subscriptions == null) 
                return;
                
            for (var i = 0; i < m_Subscriptions.Length; ++i)
                m_Subscriptions[i].Dispose();
            m_Subscriptions = null;
        }
    }

    internal sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new ();
        
        public void Dispose() { }
    }
    
    // TODO Remove this complexity since same problem as an e.g. Mux? Instead use a mux node internally.
    //      This could be the type we wrap. 
    
    // TODO This should be a serializable struct
    // TODO This is a node that doesn't really do anything?! It would just provide an aggregate subscription
    [Serializable]
    public class BindableInput<T> : IObservableInput<T> 
        where T : struct
    {
        // TODO Note that m_Bindings would box as long as input is a struct 
        [NonSerialized] private IObservableInput<T>[] m_Bindings;
        [NonSerialized] private int m_BindingCount;
        
        [NonSerialized] private int m_SubscriptionCount;
        [NonSerialized] private ObserverList2<T> m_Observers;
        
        public IDisposable Subscribe<TObserver>(TObserver observer)
            where TObserver : IObserver<T>
        {
            return Subscribe(Context.instance, observer);
        }

        public IDisposable Subscribe<TObserver>([NotNull] Context context, TObserver observer)
            where TObserver : IObserver<T>
        {
            if (m_BindingCount == 0)
                return NullDisposable.Instance;
            var aggregateSubscription = new AggregateSubscription(m_BindingCount);
            for (var i = 0; i < m_BindingCount; ++i)
                aggregateSubscription[i] = m_Bindings[i].Subscribe(context, observer);
            return aggregateSubscription;
        }

        public void AddBinding(IObservableInput<T> binding)
        {
            ArrayHelpers.AppendWithCapacity(ref m_Bindings, ref m_BindingCount, binding);
        }

        public int bindingCount => m_BindingCount;

        public void RemoveBinding(IObservableInputNode<T> binding)
        {
            if (m_BindingCount <= 0) 
                return;
            var index = Array.IndexOf(m_Bindings, binding);
            Array.Copy(m_Bindings, index + 1, m_Bindings, index, m_Bindings.Length - 1);
        }
    }
}
