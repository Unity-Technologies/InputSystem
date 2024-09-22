using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

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
// Combining this with SIMD operations (or auto-vectorization) allows for a high degree of parallelization where
// e.g. SSE2 would allow evaluation of 128 buttons in parallel using a single instruction.

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Add PressedState, then we can have
    //
    // void OnNext()
    // {
    //      
    // }
    
    // TODO Consider generalizing as specialization of Step transition for binary type.
    // Represents a press interaction
    [Serializable]
    public struct Pressed<TSource> : IObservableInputNode<InputEvent>, IUnsafeObservable<InputEvent>
        where TSource : IObservableInputNode<bool>, IDependencyGraphNode, IUnsafeObservable<bool>
    {
        [SerializeField] private TSource source;
        
        public Pressed([InputPort] TSource source)
        {
            this.source = source;
        }

        public IDisposable Subscribe(IObserver<InputEvent> observer) => Subscribe(Context.instance, observer);
        
        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<InputEvent>
        {
            // TODO When subscribing we want to check if key (proxy) has a registered instance
            //      within context and reuse this instance if it exists.
            // Do we want to compare this to impl?!
            
            // TODO HOW Can we cache nodes for a given setup? It would basically mean we need a full Equals of content including type. Since we require nodes to be structs and be Equatable we might need to store the sources within the node but then we also box them.  
            
            // TODO Attempt to get impl for source, if found subscribe to that node. If not found, create and subscribe.
            // TODO This is only about caching so we might as well just hash and see if we get a hit (likely), but the problem is if its a false positive, the problem is our observers doesn't know their source. Maybe we should take that cost while we anyway allocate the node. 
            var impl = context.GetNodeImpl<PressedObserver>(this); // TODO Instead consider getting class cache for context, m_Source is typesafe key
            if (impl == null)
            {
                impl = new PressedObserver();
                impl.Initialize(source.Subscribe(context, impl)); // TODO An alternative here would be to utilize unsafe subscription
                //context.RegisterNodeImpl(this, impl); // TODO Unable to unregister impl with this design
            }
            
            // Note that subscription is done on the actual implementation node where we register observer
            // to be invoked when impl has a new value.
            return impl.Subscribe(context, observer);
            
            // return (m_Impl ??= new Impl(context, m_Source)).Subscribe(context, observer);
        }
            
        // TODO Reader end-point
        // TODO Provide a non-abstract enumerator or let reader implement IEnumerable
        public IEnumerable<InputEvent> Subscribe2(Context context)
        {
            // TODO m_Source.GetEnumerator();
            
            // TODO Need additional constraint on source so that we can unroll into operations
            return new Reader(source);
            //return new Reader<InputEvent, Impl>(GetImplementation(context));
        }

        public unsafe UnsafeSubscription Subscribe(Context context, UnsafeDelegate<InputEvent> observer) // TODO Should take observer (receiver)?
        {
            // Initialize state
            var pressed = UnsafePressed.Create(context, source, false); // TODO GetorCreate?
            return UnsafePressed.Subscribe(pressed, observer);
        }
        
        // TODO Yet another alternative would be to register cached delegate and pass state by ref
        // This way we avoid action/lambda overhead by using a single cached static Action and pass state via reference
        // E.g. delegate void Observer(T, ref State)
        // State would then need to contain other handlers which limits its usefulness since we cannot store them in unmanaged struct.

        public readonly struct Reader : IReader<InputEvent>
        {
            //private readonly Impl m_Impl;
            private readonly TSource m_Source;
            
            internal Reader(TSource source)
            {
                m_Source = source; // Source should also be enumerable
            }
            
            public IEnumerator<InputEvent> GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        // TODO Need Reader<T> and SequenceReader<T> if we should even support it. Lets skip it.

        public bool Equals(IDependencyGraphNode other) => other is Pressed<TSource> pressed && Equals(pressed);
        public bool Equals(Pressed<TSource> other) => source.Equals(other.source);
        
        public string displayName => "Pressed"; // TODO Could be optional attribute and use type name when not defined
        public int childCount => 1; // TODO Could be detected by presence of attributes on properties
        public IDependencyGraphNode GetChild(int index) =>
            index == 0 ? source : throw new ArgumentOutOfRangeException(nameof(index)); // TODO Problematic if attribute unless defined that if there are at least one subscription attempting to change source will throw, or we can use constructor to detect IObservable inputs passed to iut, or use a factory similar to JavaFX to keep it immutable but provide a detectable interface.
    }
    
    /// <summary>
    /// Allows applying Press interaction evaluation on an observable source.
    /// </summary>
    public static class PressedExtensionMethods
    {
        /// <summary>
        /// Returns a Press interaction taking <c>bool</c> as input and generating <c>InputEvent</c> as output.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <returns>Press interaction using a <typeparamref name="TSource"/> type.</returns>
        //[InputNodeFactory(type=typeof(Pressed<IObservableInput<bool>>))]
        public static Pressed<TSource> Pressed<TSource>(this TSource source)
            where TSource : IObservableInputNode<bool>, IDependencyGraphNode, IUnsafeObservable<bool>
        {
            return new Pressed<TSource>(source);
        }
    }
    
        // ObservableInput<InputEvent>
    // TODO State could be unmanaged from pool
    // TODO ObserverList could be from pool
    // TODO Impl could be pooled (need interface)
    //
    // TODO Should register itself to avoid duplicate nodes
    // TODO An alternative would be to subscribe to underlying stream with an action that would operate on cast state
    // TODO Consider using an object handle to context and observers. We may use a bit to signify managed vs unmanageed.
    internal sealed class PressedObserver : IObserver<bool>
    {
        private bool m_PreviousValue;                  // TODO This is state
        private ObserverList2<InputEvent> m_Observers; // TODO This is effectively actions in state, we cannot store managed in unmanaged, this may cause problems. Hence we might need object handle and keep this in managed.
    
        public void Initialize(IDisposable sourceSubscription)
        {
            m_Observers = new ObserverList2<InputEvent>(sourceSubscription);
        }

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
    
        // TODO Need to expose a subscription of its own. Could simply be a delegate
    internal unsafe struct UnsafePressed
    {
        private static readonly delegate*<bool, void*, void> Next = &OnNext;
        
        private UnsafeEventHandler<InputEvent> m_OnNext;
        private bool m_PreviousValue; // Actual state
        private AllocatorManager.AllocatorHandle m_Allocator;
        private UnsafeSubscription m_TriggerSubscription; 

        public static UnsafePressed* Create<TSource>(Context context, TSource source, bool previousValue = false)
            where TSource : IUnsafeObservable<bool>
        {
            var ptr = context.unsafeContext.Allocate<UnsafePressed>();
            ptr->m_Allocator = context.unsafeContext.allocator;
            ptr->m_PreviousValue = previousValue;
            ptr->m_TriggerSubscription = source.Subscribe(context, new UnsafeDelegate<bool>(Next, ptr));
            return ptr;
        }

        // TODO If we instead pass context to this function we can hide create inside and also handle registration/allocation
        public static UnsafeSubscription Subscribe(UnsafePressed* pressed, UnsafeDelegate<InputEvent> d)
        {
            pressed->m_OnNext.Add(d);
            return new UnsafeSubscription(
                new UnsafeDelegate<UnsafeCallback>(&Unsubscribe, pressed), d.ToCallback());
        }

        private static void Unsubscribe(UnsafeCallback callback, void* state)
        {
            var s = (UnsafePressed*)state;
            ; // TODO Need to return boolean or expose length (not thread safe)
            s->m_OnNext.Remove(callback);
            if (s->m_OnNext.GetInvocationList().Length == 0) // TODO This can never be thread safe and exposes undesirable API
                Destroy(s);
        }

        private static void Destroy(UnsafePressed* state)
        {
            state->m_TriggerSubscription.Dispose();
            AllocatorManager.Free(state->m_Allocator, state, sizeof(UnsafePressed), UnsafeUtility.AlignOf<UnsafePressed>());
        }

        private static void OnNext(bool value, void* state)
        {
            OnNext(value, ref *(UnsafePressed*)state);
        }
        
        private static void OnNext(bool value, ref UnsafePressed state)
        {
            if (state.m_PreviousValue == value)
                return;
            if (value)
                state.m_OnNext.Invoke(new InputEvent());
            state.m_PreviousValue = value;
        }
    }
}
