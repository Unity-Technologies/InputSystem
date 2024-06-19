using System;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Do we even need this?
    public class BindableInput<T> : IObserver<T>, IDisposable 
        where T : struct
    {
        public delegate void Callback(T value);
        public event Callback performed;

        private Context m_Context;
        private ObservableInput<T>[] m_Bindings;
        private int m_BindingCount;
        private IDisposable[] m_Subscriptions;
        private int m_SubscriptionCount;

        public static BindableInput<T> Create(Callback callback, IObservableInput<T> source = null, Context context = null)
        {
            return new BindableInput<T>(callback, source, context);
        }

        public BindableInput(ObservableInput<T> binding, Context context = null)
        {
            m_Context = context ?? Context.instance;
            Bind(binding);
        }

        public BindableInput(IObservableInput<T> binding, Context context = null)
        {
            m_Context = context ?? Context.instance;
            Bind(binding);
        }

        public BindableInput(Callback callback, IObservableInput<T> binding = null, Context context = null)
            : this(binding, context)
        {
            performed += callback; // TODO We could dispatch this down to avoid multiple levels of indirection?
        }

        public BindableInput(Callback callback, ObservableInput<T> binding, Context context = null)
            : this(binding, context)
        {
            performed += callback;
        }

        /*public BindableInput(Callback callback, IObservable<T> binding, Context context = null)
        {
            m_Context = context;
            Bind(binding);

            performed += callback;
        }*/

        public static implicit operator BindableInput<T>(ObservableInput<T> source)
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

        public void Bind<TSource>(TSource source) where TSource : IObservableInput<T>
        {
            ArrayHelpers.AppendWithCapacity(ref m_Subscriptions, ref m_SubscriptionCount,
                source.Subscribe(m_Context, this));
        }

        /*public void Bind(InputBindingSource<T> source)
        {
            ArrayHelpers.AppendWithCapacity(ref m_Bindings, ref m_BindingCount, source);
        }*/

        // TODO Consider requiring context to be passed and instead only make IObservable<T> out of context owning objects
        public void Bind(IObservableInput<T> observable)
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
