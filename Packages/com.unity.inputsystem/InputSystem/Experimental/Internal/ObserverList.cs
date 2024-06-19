using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        private IDisposable[] m_Disposables;
        
        public ObserverList2(Action onUnsubscribed = null) // TODO Replace with IDisposable list
        {
            m_Observers = null;
            m_OnUnsubscribed = onUnsubscribed;
        }

        public ObserverList2(IDisposable disposable)
            : this(new [] { disposable })
        { }
        
        public ObserverList2(IDisposable disposable1, IDisposable disposable2)
            : this(new [] { disposable1, disposable2 })
        { }

        public ObserverList2(IDisposable[] disposables)
        {
            m_Observers = null;
            m_Disposables = disposables;
        }

        // TODO Consider caching subscriptions globally
        // TODO Additionally, if always returning this subscription type we might as well avoid IDisposable
        private sealed class Subscription : IDisposable
        {
            private ObserverList2<T> m_Owner;
            private IObserver<T> m_Observer;

            public Subscription(ObserverList2<T> owner, IObserver<T> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }

            public void Dispose()
            {
                // Abort if already disposed
                if (m_Owner == null)
                    return;
                
                // Remove observer
                m_Owner.Remove(m_Observer);
                
                // Allow owner and observer to be collected
                m_Owner = null;
                m_Observer = null;
            }
        }

        private void Remove(IObserver<T> observer)
        {
            if (!m_Observers.Remove(observer))
                throw new Exception("Unexpected error");
            if (m_Observers.Count == 0)
            {
                if (m_OnUnsubscribed != null)
                    m_OnUnsubscribed.Invoke();
                if (m_Disposables != null)
                {
                    for (var i=0; i < m_Disposables.Length; ++i)
                        m_Disposables[i].Dispose();
                }
            }
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
                m_Observers[i].OnError(e);
        }

        public void OnNext(T value)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnNext(value);
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            m_Observers ??= new List<IObserver<T>>(1); // TODO Opportunity to cache observers in context-specific array
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
            private ObserverList<T> m_Owner; // TODO This doesn't work, its a copy
            private IObserver<T> m_Observer;

            public Subscription([NotNull] ObserverList<T> owner, [NotNull] IObserver<T> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }

            public void Dispose()
            {
                m_Owner.Remove(m_Observer);
                //m_Owner = null;
                m_Observer = null;
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
            if (m_Observers == null)
                return;
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnError(Exception e)
        {
            if (m_Observers == null)
                return;
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnNext(T value)
        {
            if (m_Observers == null)
                return;
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
