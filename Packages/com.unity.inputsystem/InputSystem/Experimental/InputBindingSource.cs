using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider if this is an anti-pattern due to the facts that it creates a binding with an
    // operation on it that prevents seeing observables as a single type if though implementing interfacees.
    // Instead consider an object defining inputs and outputs which decouples this and removes the restriction
    // of single input/output. It basically needs to implement filter
    internal class DerivedObservable<TIn, TOut> : IObserver<TIn>, IObservable<TOut> where TIn : struct
    {
        private InputBindingSource<TIn> m_Source;
        private IDisposable m_Subscription;
        private List<IObserver<TOut>> m_Observers;
        private Func<TIn, TOut> m_Func;

        public DerivedObservable(InputBindingSource<TIn> source, Func<TIn, TOut> func)
        {
            m_Source = source;
            m_Func = func;
        }
        
        // TODO We could let context be responsible and use a subscription id to avoid instances
        private class Subscription : IDisposable
        {
            private readonly DerivedObservable<TIn, TOut> m_Owner;
            private readonly IObserver<TOut> m_Observer;
                
            public Subscription(DerivedObservable<TIn, TOut> owner, IObserver<TOut> observer)
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
            for (var i=0; i < m_Observers.Count; ++i)
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
    
    // TODO This needs to be hidden somehow, otherwise we are forced to use an interface and
    //      proxies become less useful since they require heap memory allocation.
    //      It might be difficult to avoid this unless we derive a custom ObservableBindingSource.
    
    /// <summary>
    /// Acts as a proxy for a derived input binding source.
    /// </summary>
    /// <typeparam name="TIn">The input type</typeparam>
    /// <typeparam name="TOut">The output type</typeparam>
    public struct DerivedInputBindingSource<TIn, TOut> : IObservable<TOut>
        where TIn : struct
        where TOut : struct
    {
        private readonly InputBindingSource<TIn> m_Source;
        private readonly Func<TIn, TOut> m_Func;
        private DerivedObservable<TIn, TOut> m_DerivedObservable;
        
        public DerivedInputBindingSource(InputBindingSource<TIn> source, Func<TIn, TOut> func)
        {
            m_Source = source;
            m_Func = func;
            m_DerivedObservable = null;
        }
        
        public IDisposable Subscribe(Context context, IObserver<TOut> observer)
        {
            m_DerivedObservable ??= new DerivedObservable<TIn, TOut>(m_Source, m_Func);
            return m_DerivedObservable.Subscribe(context, observer);
        }

        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            return Subscribe(Context.instance, observer);
        }
        
        // TODO Implicit conversion to TOut possible or only for button?
    }

    public interface IInputBindingSource<T> : IObservable<T> where T : struct
    {
        public IDisposable Subscribe([NotNull] Context ctx, IObserver<T> observer);
    }
    
    // TODO This makes sense when usage uniquely identifies a stream.
    //      This makes less sense for a derived binding source unless cached.
    //      However, cached behavior is enforced if done via DOTS so maybe
    //      we should compute into memory by default and avoid complexity?
    //      Should likely compare the two variants in a benchmark.
    //      Stream memory could easily be allocated from a native memory pool.
    // A binding source associated with a usage (and and an associated stream)
    public readonly struct InputBindingSource<T> : IObservable<T> where T : struct
    {
        // Note: We could have avoided unsafe here if we had ref fields (C# 11)

        private readonly Usage m_Usage;
        
        public InputBindingSource(Usage usage)
            : this(Context.instance, usage)
        { }
        
        public InputBindingSource(Context ctx, Usage usage)
        {
            if (usage.Value == Usage.Invalid.Value) // TODO FIX
                throw new ArgumentException(nameof(usage));
                
            m_Usage = usage;
        }
        
        public IDisposable Subscribe([NotNull] Context ctx, IObserver<T> observer)
        {
            // Subscribe to source by subscribing to a stream context.
            // Note that this is the approach we should use for any cached or underlying data.
            return ctx.GetOrCreateStreamContext<T>(m_Usage).Subscribe(observer);
        }
        
        public IDisposable Subscribe([NotNull] IObserver<T> observer)
        {
            return Subscribe(Context.instance, observer);
        }
    }
}