using System;

namespace UnityEngine.InputSystem.Experimental
{
    public struct Held<TSource> : IObservableInput<InputEvent>
        where TSource : IObservableInput<bool>, IDependencyGraphNode
    {
        internal sealed class Impl : IObserver<bool>
        {
            private bool m_PreviousValue;
            private uint m_Timestamp;
            private readonly ObserverList2<InputEvent> m_Observers;
        
            public Impl(Context context, TSource source)
            {
                m_Observers = new ObserverList2<InputEvent>(source.Subscribe(context, this));
            }

            public IDisposable Subscribe(IObserver<InputEvent> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must unless we use a base

            public IDisposable Subscribe(Context context, IObserver<InputEvent> observer) =>
                m_Observers.Subscribe(context, observer);
            
            public void OnCompleted() => m_Observers.OnCompleted();
            public void OnError(Exception error) => m_Observers.OnError(error);

            public void OnNext(bool value)
            {
                if (m_PreviousValue == value) 
                    return;
                if (value) // TODO Let class be converted to Step and take a IComparable<T> type, then we can use for both Press and Relase
                    m_Observers.OnNext(new InputEvent()); // TODO This needs to be tentative, should indirection between data observer an events or we need another stage, so its either a separate method or parameter
                m_PreviousValue = value;
            }
        }

        private readonly TSource m_Source;    // The source to observe for press events
        private readonly TimeSpan m_Duration; // The duration for which trigger must be happy.
        private Impl m_Impl;                  // Implementation, lazily constructed
        
        public Held([InputPort] TSource source, TimeSpan duration)
        {
            m_Source = source;
            m_Duration = duration;
            m_Impl = null;
        }
        
        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public IDisposable Subscribe(Context context, IObserver<InputEvent> observer)
        {
            var impl = context.GetNodeImpl<Impl>(this); // TODO Instead consider getting class cache for context, m_Source is typesafe key
            if (impl == null)
            {
                impl = new Impl(context, m_Source);
                context.RegisterNodeImpl(this, impl); // TODO Unable to unregister impl with this design
            }

            return impl.Subscribe(context, observer);
        }

        public bool Equals(IDependencyGraphNode other)
        {
            throw new NotImplementedException();
        }

        public string displayName => "Held";
        public int childCount => 2;
        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Source;
                case 1: return null; // TODO Time
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    public static class HeldExtensions
    {
        
    }
}