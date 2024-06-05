using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Acts as a proxy for a derived input binding source based off a lambda or other indirect System.Func.
    /// </summary>
    /// <typeparam name="TIn">The input type</typeparam>
    /// <typeparam name="TOut">The output type</typeparam>
    public struct DerivedInputBindingSource<TIn, TOut> : IInputBindingSource<TOut>
        where TIn : struct
        where TOut : struct
    {
        private sealed class DerivedObservable : IObserver<TIn>, IObservable<TOut>
        {
            private readonly InputBindingSource<TIn> m_Source;
            private readonly Func<TIn, TOut> m_Func;
            private IDisposable m_Subscription;
            private List<IObserver<TOut>> m_Observers;

            public DerivedObservable(InputBindingSource<TIn> source, Func<TIn, TOut> func)
            {
                m_Source = source;
                m_Func = func;
            }

            // TODO We could let context be responsible and use a subscription id to avoid instances
            private class Subscription : IDisposable
            {
                private readonly DerivedObservable m_Owner;
                private readonly IObserver<TOut> m_Observer;

                public Subscription(DerivedObservable owner, IObserver<TOut> observer)
                {
                    m_Owner = owner;
                    m_Observer = observer;
                }

                public void Dispose()
                {
                    m_Owner.m_Observers.Remove(m_Observer);
                    if (m_Owner.m_Observers.Count == 0)
                    {
                        m_Owner.m_Subscription.Dispose();
                        m_Owner.m_Subscription = null;
                    }
                }
            }

            public void OnCompleted()
            {
                throw new NotImplementedException();
            }

            public void OnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public void OnNext(TIn value)
            {
                if (m_Observers == null)
                    return;

                var derived = m_Func(value);
                for (var i = 0; i < m_Observers.Count; ++i)
                    m_Observers[i].OnNext(derived);
            }

            public IDisposable Subscribe(Context context, IObserver<TOut> observer)
            {
                if (m_Observers == null)
                    m_Observers = new List<IObserver<TOut>>();
                if (m_Observers.Count == 0)
                    m_Subscription = m_Source.Subscribe(context, this);
                m_Observers.Add(observer);
                return new Subscription(this, observer);
            }

            public IDisposable Subscribe(IObserver<TOut> observer)
            {
                return Subscribe(Context.instance, observer);
            }
        }

        private readonly InputBindingSource<TIn> m_Source;
        private readonly Func<TIn, TOut> m_Func;
        private DerivedObservable m_DerivedObservable;

        public DerivedInputBindingSource(InputBindingSource<TIn> source, Func<TIn, TOut> func)
        {
            m_Source = source;
            m_Func = func;
            m_DerivedObservable = null;
        }

        public IDisposable Subscribe(Context context, IObserver<TOut> observer)
        {
            m_DerivedObservable ??= new DerivedObservable(m_Source, m_Func);
            return m_DerivedObservable.Subscribe(context, observer);
        }

        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        // TODO Implicit conversion to TOut possible or only for button?
    }
}
