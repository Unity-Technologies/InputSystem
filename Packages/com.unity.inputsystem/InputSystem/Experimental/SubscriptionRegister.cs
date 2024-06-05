using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider if we should allocate observers in a memory pool to avoid
    //      allocations.

    /*internal struct Ob

    internal static class ObserverListExtensions
    {
        private sealed class ListSubscription<T> : IDisposable
        {
            private readonly List<IObserver<T>> m_Owner;
            private readonly IObserver<T> m_Observer;

            public ListSubscription(List<IObserver<T>> owner, IObserver<T> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }

            public void Dispose()
            {
                m_Owner.Remove(m_Observer);
            }
        }

        public static IDisposable Subscribe<T>(this List<IObserver<T>> list, IObserver<T> observer)
        {
            list.Add(observer);
            return new ListSubscription<T>(list, observer);
        }
    }*/

    internal struct ObserverList<T>
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

        private void Remove(IObserver<T> observer)
        {
            if (!m_Observers.Remove(observer))
                throw new Exception("Unexpected error");
            if (m_Observers.Count == 0 && m_OnUnsubscribed != null)
                m_OnUnsubscribed.Invoke();
        }

        public bool Empty => m_Observers.Count == 0;

        public int Count => m_Observers.Count;

        public void OnCompleted()
        {
            for (var i = 0; i < m_Observers.Count; ++i)
            {
                m_Observers[i].OnCompleted();
            }
        }

        public void OnError(Exception e)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
            {
                m_Observers[i].OnCompleted();
            }
        }

        public void OnNext(T value)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
            {
                m_Observers[i].OnNext(value);
            }
        }

        public IDisposable Add(IObserver<T> observer)
        {
            m_Observers ??= new List<IObserver<T>>(1);
            m_Observers.Add(observer);
            return new Subscription(this, observer);
        }
    }
}
