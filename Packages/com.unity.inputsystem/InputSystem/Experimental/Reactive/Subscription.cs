using System;
using System.Collections.Concurrent;
using UnityEngine.InputSystem.Experimental.Internal;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IUnsubscribe<out T>
    {
        public void Unsubscribe(IObserver<T> observer);
    }
    
    public struct Subscription<T> : IDisposable 
        where T : struct
    {
        private InternalSubscription<T> m_Subscription;
        private readonly int m_Incarnation;
        
        internal Subscription(IUnsubscribe<T> subscriptionSource, IObserver<T> observer)
        {
            m_Subscription = ObjectPool<InternalSubscription<T>>.shared.Rent();
            m_Subscription.Initialize(subscriptionSource, observer);
            m_Incarnation = m_Subscription.Incarnation; // store incarnation id when subscribed
        }

        public void Dispose()
        {
            if (m_Subscription == null)
                return; // already disposed
            if (m_Incarnation != m_Subscription.Incarnation)
                return; // attempting to dispose via a reused copy
            
            m_Subscription.Dispose();
            m_Subscription = null;
        }
    }

    internal static class InternalSubscription
    {
        public static int WrapAroundCounter = 0;
    }
    
    internal sealed class InternalSubscription<T> : IDisposable
        where T : struct
    {
        private IUnsubscribe<T> m_SubscriptionSource;
        private IObserver<T> m_Observer;

        public void Initialize(IUnsubscribe<T> subscriptionSource, IObserver<T> observer)
        {
            m_SubscriptionSource = subscriptionSource;
            m_Observer = observer;
            ++Incarnation; // Note: Prevents a reused subscription
        }

        public int Incarnation { get; private set; } = ++InternalSubscription.WrapAroundCounter;

        public void Dispose()
        {
            if (m_SubscriptionSource == null)
                return;
            
            ++Incarnation; // Mutate to prevent reused subscription dispose if subscription struct was copied
            m_SubscriptionSource.Unsubscribe(m_Observer);
            m_SubscriptionSource = null;
            m_Observer = null;
        }
    }
}