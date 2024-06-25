using System;
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
    public struct Pressed<TSource> : IObservableInput<InputEvent>, IDependencyGraphNode
        where TSource : IObservableInput<bool>, IDependencyGraphNode
    {
        private sealed class Impl : IObserver<bool>
        {
            private bool m_PreviousValue;
            private readonly ObserverList2<InputEvent> m_Observers;
        
            public Impl(Context context, TSource source)
            {
                m_Observers = new ObserverList2<InputEvent>(source.Subscribe(context, this));
            }

            public IDisposable Subscribe(IObserver<InputEvent> observer) => 
                Subscribe(Context.instance, observer); // TODO Unnecessary must

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
        
        private readonly TSource m_Source;
        private Impl m_Impl;
        
        public Pressed([InputPort] TSource source)
        {
            m_Source = source;
            m_Impl = null;
        }

        public IDisposable Subscribe(IObserver<InputEvent> observer) =>
            Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<InputEvent> observer) =>
            (m_Impl ??= new Impl(context, m_Source)).Subscribe(context, observer);
        
        // TODO Reader end-point
        public IDisposable Subscribe2(Context context)
        {
            // TODO Need additional constraint on source so that we can unroll into operations
        }
        
        public bool Equals(IDependencyGraphNode other) =>
            this.CompareDependencyGraphs(other);
        
        public string displayName => "Pressed"; // TODO Could be optional attribute and use type name when not defined
        public int childCount => 1; // TODO Could be detected by presence of attributes on properties
        public IDependencyGraphNode GetChild(int index) =>
            index == 0 ? m_Source : throw new ArgumentOutOfRangeException(nameof(index)); // TODO Problematic if attribute unless defined that if there are at least one subscription attempting to change source will throw, or we can use constructor to detect IObservable inputs passed to iut, or use a factory similar to JavaFX to keep it immutable but provide a detectable interface.
    }

    /*public struct PressJob : IJob
    {
        public PressJob(Stream<T>)
        {
            
        }
        
        public void Execute()
        {
            
        }

        public static void Schedule()
        {
            var job = new PressJob();
        }
    }*/
    
    internal static class PressSeq
    {
        public static unsafe void Press(bool* first, bool* last, Stream<InputEvent> result)
        {
            if (first == last) 
                return;
            var prev = *first++;
            for (; first != last; ++first)
            {
                var current = *first;
                if (current != prev && current)
                    result.OfferByValue(new InputEvent());
                prev = current;
            }
        }
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
