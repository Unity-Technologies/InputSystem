using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;

// TODO See comments in header
// TODO Should we be allowed to construct a node that isn't properly connected? E.g. null source.

// For a binary type, press edges (transition from 0 to 1) may be detected by comparing two adjacent numbers as such:
// 
//       press(x, i) = x[i] & (x[i] ^ x[i-1]), where i > 0
//
// For a binary type, release edges (transition from 1 to 0) may be detected by comparing two adjacent numbers as such:
//
//      release(x,i) = x[i] & (x[i] ^ x[i-1]), where i > 0
//
// Time  Previous  Current  XOR     PRESS    RELEASE
// t     p         c        p^c     (p^c)&c  (p^c)&(!c)
// 0     n/a       false    n/a     n/a      n/a
// 1     false     true     true    true     false
// 2     true      true     false   false    false
// 3     true      false    true    false    true
// 4     false     false    false   false    false
//
// If buttons are encoded as a bit fields, press and release may be parallelized by bitwise operations even when not
// using SIMD. See example below for 4 buttons encoded as 4 bits:
//
// Time  Previous  Current  XOR     PRESS    RELEASE
// t     p         c        p^c     (p^c)&c  (p^c)&(~c)
// 0     n/a       0100     n/a     n/a      n/a
// 1     0100      1100     1000    1000     0000     
// 2     1100      1110     0010    0010     0000
// 3     1110      1100     0010    0000     0010
//
// Combining this with SIMD operations (or auto-vectorization) allows for a high degree of parallelization

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider generalizing as specialization of Step transition for binary type.
    // Represents a press interaction
    public struct Pressed<TSource> : IObservableInput<InputEvent>
        where TSource : IObservableInput<bool>, IDependencyGraphNode
    {
        // ObservableInput<InputEvent>
        // TODO State could be unmanaged from pool
        // TODO ObserverList could be from pool
        // TODO Impl could be pooled (need interface)
        //
        // TODO Should register itself to avoid duplicate nodes
        // TODO An alternative would be to subscribe to underlying stream with an action that would operate on cast state
        internal sealed class Impl : IObserver<bool>
        {
            private bool m_PreviousValue;                           // TODO This is state
            private readonly ObserverList2<InputEvent> m_Observers; // TODO This is effectively actions
        
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
        
        private readonly TSource m_Source;  // The source to observe for press events
        private Impl m_Impl;                // Implementation, lazily constructed
        
        public Pressed([InputPort] TSource source)
        {
            m_Source = source;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            return Subscribe(Context.instance, observer);
        }
        
        public IDisposable Subscribe(Context context, IObserver<InputEvent> observer)
        {
            // TODO When subscribing we want to check if key (proxy) has a registered instance
            //      within context and reuse this instance if it exists.
            // Do we want to compare this to impl?!
            
            // TODO Attempt to get impl for source, if found subscribe to that node. If not found, create and subscribe.
            var impl = context.GetNodeImpl<Impl>(this); // TODO Instead consider getting class cache for context, m_Source is typesafe key
            if (impl == null)
            {
                impl = new Impl(context, m_Source);
                context.RegisterNodeImpl(this, impl); // TODO Unable to unregister impl with this design
            }

            return impl.Subscribe(context, observer);
            
            // return (m_Impl ??= new Impl(context, m_Source)).Subscribe(context, observer);
        }
            
        // TODO Reader end-point
        // TODO Provide a non-abstract enumerator or let reader implement IEnumerable
        public IEnumerable<InputEvent> Subscribe2(Context context)
        {
            // TODO m_Source.GetEnumerator();
            
            // TODO Need additional constraint on source so that we can unroll into operations
            return new Reader(m_Source);
            //return new Reader<InputEvent, Impl>(GetImplementation(context));
        }

        public readonly struct Reader : IReader<InputEvent>
        {
            //private readonly Impl m_Impl;
            private readonly TSource m_Source;
            
            internal Reader(TSource source)
            {
                m_Source = source; // Source should also be enumerable
            }
            
            public IEnumerator<InputEvent> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public bool Equals(IDependencyGraphNode other)
        {
            return other is Pressed<TSource> pressed && Equals(pressed);
        }

        public bool Equals(Pressed<TSource> other)
        {   
            return m_Source.Equals(other.m_Source);
        }
        
        public string displayName => "Pressed"; // TODO Could be optional attribute and use type name when not defined
        public int childCount => 1; // TODO Could be detected by presence of attributes on properties
        public IDependencyGraphNode GetChild(int index) =>
            index == 0 ? m_Source : throw new ArgumentOutOfRangeException(nameof(index)); // TODO Problematic if attribute unless defined that if there are at least one subscription attempting to change source will throw, or we can use constructor to detect IObservable inputs passed to iut, or use a factory similar to JavaFX to keep it immutable but provide a detectable interface.
    }
    
    /// <summary>
    /// Allows applying Press interaction evaluation on an observable source.
    /// </summary>
    public static class PressedExtensionMethods
    {
        /// <summary>
        /// Returns a Press interaction operating on a source of type <typeparamref name="TSource"/>.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <returns>Press interaction using a <typeparamref name="TSource"/> type.</returns>
        [InputNodeFactory(type=typeof(Pressed<IObservableInput<bool>>))]
        public static Pressed<TSource> Pressed<TSource>(this TSource source)
            where TSource : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Pressed<TSource>(source);
        }
    }
}
