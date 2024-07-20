using System;
using System.Threading;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO We need want a timer callback when sufficient time has passed.
    //      This is either driven by processing an event with timestamp passed or timer event if no callback.
    //      The best implementation is likely native timer output.
    public readonly struct Held<TSource> : IObservableInput<InputEvent>
        where TSource : IObservableInput<bool>
    {
        // TODO This is a class only to be able to receive callbacks via IObserver<bool>
        // TODO This could be different with a delegate
        internal sealed class Impl : IObserver<bool>
        {
            // State
            private bool m_PreviousValue;
            private uint m_Timestamp;
            
            private readonly ObserverList2<InputEvent> m_Observers;
            
            public Impl(Context context, TSource source)
            {
                m_Observers = new ObserverList2<InputEvent>(source.Subscribe(context, this));
                //context.CreateTimer();
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
                    return; // No change
                m_PreviousValue = value;
                if (value) // TODO Let class be converted to Step and take a IComparable<T> type, then we can use for both Press and Relase
                {
                    // TODO Start timer with dueTime matching press event timestamp
                }
                else
                {
                    // TODO Stop timer
                }
            }

            public void ForwardOnNext(uint timestamp)
            {
                m_Observers.OnNext(new InputEvent()); // TODO This needs to be tentative, should indirection between data observer an events or we need another stage, so its either a separate method or parameter
            }
        }

        private readonly TSource m_Source;    // The source to observe for press events
        private readonly TimeSpan m_Duration; // The duration for which trigger must be happy.
        
        public Held([InputPort] TSource source, TimeSpan duration)
        {
            m_Source = source;
            m_Duration = duration;
        }
        
        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        //internal delegate Impl Factory(Context context, in TSource source);
        //private static Func<Held, out Impl> factory = Create;

        /*private static Impl Create(Context context, Held<TSource> prototype)
        {
            return new Impl(context, prototype.m_Source);
        }*/
        
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

    /// <summary>
    /// Allows applying Hold interaction evaluation on an observable source.
    /// </summary>
    public static class HeldExtensionMethods
    {
        /// <summary>
        /// Returns a Held interaction operating on a source of type <typeparamref name="TSource"/>.
        /// </summary>
        /// <param name="source">The trigger source.</param>
        /// <param name="duration">The duration for which the trigger must be kept active before the event fires.</param>
        /// <typeparam name="TSource">The trigger source type.</typeparam>
        /// <returns>Hold interaction using a <typeparamref name="TSource"/> type.</returns>
        [InputNodeFactory(type=typeof(Held<IObservableInput<bool>>))]
        public static Held<TSource> Held<TSource>(this TSource source, TimeSpan duration)
            where TSource : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Held<TSource>(source, duration);
        }
    }
}