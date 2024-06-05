using System;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    public class BindableInput<T> : IObserver<T>, IDisposable where T : struct
    {
        public delegate void Callback(T value);
        public event Callback performed;

        private Context m_Context;
        private InputBindingSource<T>[] m_Bindings;
        private int m_BindingCount;
        private IDisposable[] m_Subscriptions;
        private int m_SubscriptionCount;

        public static BindableInput<T> Create(Callback callback, IInputBindingSource<T> source = null, Context context = null)
        {
            return new BindableInput<T>(callback, source, context);
        }

        public BindableInput(InputBindingSource<T> binding, Context context = null)
        {
            m_Context = context ?? Context.instance;
            Bind(binding);
        }

        public BindableInput(IInputBindingSource<T> binding, Context context = null)
        {
            m_Context = context ?? Context.instance;
            Bind(binding);
        }

        public BindableInput(Callback callback, IInputBindingSource<T> binding = null, Context context = null)
            : this(binding, context)
        {
            performed += callback; // TODO We could dispatch this down to avoid multiple levels of indirection?
        }

        public BindableInput(Callback callback, InputBindingSource<T> binding, Context context = null)
            : this(binding, context)
        {
            performed += callback;
        }

        public BindableInput(Callback callback, IObservable<T> binding, Context context = null)
        {
            m_Context = context;
            Bind(binding);

            performed += callback;
        }

        public static implicit operator BindableInput<T>(InputBindingSource<T> source)
        {
            return new BindableInput<T>(source);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            Debug.LogException(error);
        }

        public void OnNext(T value)
        {
            performed?.Invoke(value);
        }

        public void Bind(IInputBindingSource<T> source)
        {
            var subscription = source.Subscribe(this);
            ArrayHelpers.AppendWithCapacity(ref m_Subscriptions, ref m_SubscriptionCount, subscription);
        }

        public void Bind(InputBindingSource<T> source)
        {
            ArrayHelpers.AppendWithCapacity(ref m_Bindings, ref m_BindingCount, source);
        }

        // TODO Consider requiring context to be passed and instead only make IObservable<T> out of context owning objects
        public void Bind(IObservable<T> observable)
        {
            var subscription = observable.Subscribe(this);
            ArrayHelpers.AppendWithCapacity(ref m_Subscriptions, ref m_SubscriptionCount, subscription);
        }

        public void Dispose()
        {
            for (var i = 0; i < m_SubscriptionCount; ++i)
            {
                m_Subscriptions[i].Dispose();
                m_Subscriptions[i] = null;
            }
            m_SubscriptionCount = 0;
        }
    }
}
