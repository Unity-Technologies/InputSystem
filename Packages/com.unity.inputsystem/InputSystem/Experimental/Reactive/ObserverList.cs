using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider backing ObserverList by a memory pool to reduce allocations
    // TODO Consider pooling both list and subscriptions. Subscription could be simplified if mapping subscription to subscription keys. A subscription key may allow tracking both the source and a derivative from the source.
    // TODO Consider if this should be a class.
    // TODO Consider if instead of have onUnsubscribed, this could instead be passed sources to subscribe to and handle unsubscribe for us?

    internal class ObserverList2<T> : IObserver<T>
    {
        private List<IObserver<T>> m_Observers;
        private readonly Action m_OnUnsubscribed;

        public ObserverList2(Action onUnsubscribed = null)
        {
            m_Observers = null;
            m_OnUnsubscribed = onUnsubscribed;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ObserverList2<T> m_Owner;
            private readonly IObserver<T> m_Observer;

            public Subscription(ObserverList2<T> owner, IObserver<T> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }

            public void Dispose()
            {
                m_Owner.Remove(m_Observer);
            }
        }

        private void Remove(IObserver<T> observer)
        {
            if (!m_Observers.Remove(observer))
                throw new Exception("Unexpected error");
            if (m_Observers.Count == 0 && m_OnUnsubscribed != null)
                m_OnUnsubscribed.Invoke();
        }

        public bool empty => m_Observers == null || m_Observers.Count == 0;

        public int count => m_Observers?.Count ?? 0;

        public void OnCompleted()
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnError(Exception e)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnNext(T value)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnNext(value);
        }

        public IDisposable Add(IObserver<T> observer)
        {
            m_Observers ??= new List<IObserver<T>>(1);
            m_Observers.Add(observer);
            return new Subscription(this, observer);
        }
    }    
    
    internal struct ObserverList<T> : IObserver<T>
    {
        private List<IObserver<T>> m_Observers;
        private readonly Action m_OnUnsubscribed;

        public ObserverList(Action onUnsubscribed = null)
        {
            m_Observers = null;
            m_OnUnsubscribed = onUnsubscribed;
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ObserverList<T> m_Owner;
            private readonly IObserver<T> m_Observer;

            public Subscription(ObserverList<T> owner, IObserver<T> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }

            public void Dispose()
            {
                m_Owner.Remove(m_Observer);
            }
        }

        private readonly void Remove(IObserver<T> observer)
        {
            if (!m_Observers.Remove(observer))
                throw new Exception("Unexpected error");
            if (m_Observers.Count == 0 && m_OnUnsubscribed != null)
                m_OnUnsubscribed.Invoke();
        }

        public bool empty => m_Observers == null || m_Observers.Count == 0;

        public int count => m_Observers?.Count ?? 0;

        public void Clear()
        {
            m_Observers = null;
            m_OnUnsubscribed?.Invoke();
        }
        
        public void OnCompleted()
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnError(Exception e)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnNext(T value)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnNext(value);
        }

        public IDisposable Add(IObserver<T> observer)
        {
            m_Observers ??= new List<IObserver<T>>(1);
            m_Observers.Add(observer);
            return new Subscription(this, observer);
        }
    }
}
