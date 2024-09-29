using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    public partial class Context
    {
        internal abstract class StreamContext : IDisposable // TODO Non-ideal since virtual calls, try to eliminate
        {
            public abstract void Advance(); // TEMP
            public abstract void Process(); // TEMP
            public abstract void Dispose();
        }

        internal unsafe class UnsafeStreamContext
        {
            
        }

        // TODO Replace observers with observerList?
        // TODO Implement IAsyncEnumerable<T>?
        // TODO Consider having support for inteface oriented sub-streams by default.
        // For example, for keyboard each usage ID would map to bit in the usage corresponding to the interface, e.g. 0x09, hence, we could require
        // that observers are feeded  
        internal sealed class StreamContext<T> : StreamContext, IObservable<T>, IDisposable, IEnumerable<T>
            where T : struct
        {
            private readonly Endpoint m_Usage;         // The associated usage
            private IObserver<T>[] m_Observers;     // The associated observers of this stream
            private int m_ObserverCount;            // The current observer count
            private Stream<T> m_Stream;             // Reference to associated underlying stream (if any)

            public StreamContext(Endpoint usage, Stream<T> stream = null)
                : base()
            {
                m_Usage = usage;
                m_Stream = stream;
            }

            //public Stream<T> stream => Stream as Stream<T>;

            public int observerCount => m_ObserverCount;
            
            public bool hasStream => m_Stream != null;
            
            public void SetStream(Stream<T> stream)
            {
                m_Stream = stream;
            }

            internal Stream<T> GetStream()
            {
                return m_Stream;
            }

            internal Stream<T> Stream;

            /*public void Offer(ref T value)
            {
                m_Stream.Offer(ref value);
            }*/

            public override void Advance() // TODO Temp remove
            {
                if (m_Stream == null)
                    return;

                m_Stream.Advance();
            }

            public override void Process()
            {
                if (m_Stream == null)
                    return;

                // TODO Consider order here, complete for one observer vs in-order (still partial)
                // Drive update by propagating this data through the dependency tree (callbacks)
                var span = m_Stream.AsSpan();
                for (var i = 0; i < span.Length; ++i)
                {
                    // Call all observers of this stream
                    for (var j = 0; j < m_ObserverCount; ++j)
                    {
                        m_Observers[j].OnNext(span[i]);
                    }
                }
            }

            public void OnNext(T value)
            {
                for (var j = 0; j < m_ObserverCount; ++j)
                {
                    m_Observers[j].OnNext(value);
                }
            }

            public void OnNext(ref T value)
            {
                for (var j = 0; j < m_ObserverCount; ++j)
                {
                    m_Observers[j].OnNext(value);
                }
            }

            private class Subscription : IDisposable
            {
                private readonly StreamContext<T> m_Owner;
                private readonly IObserver<T> m_Observer;

                public Subscription(StreamContext<T> owner, IObserver<T> observer)
                {
                    m_Owner = owner;
                    m_Observer = observer;
                }

                public void Dispose()
                {
                    m_Owner.Unsubscribe(m_Observer);
                }
            }

            private void Unsubscribe(IObserver<T> observer)
            {
                // TODO Should check if disposed?
                ArrayHelpers.Erase(ref m_Observers, observer);
                --m_ObserverCount;
            }

            public IDisposable Subscribe(IObserver<T> observer) // TODO Only allow ObservableInputNode? If we pass field information into this we can utilize that
            {
                ArrayHelpers.AppendWithCapacity<IObserver<T>>(ref m_Observers, ref m_ObserverCount, observer);
                return new Subscription(this, observer);
            }

            #region Unsafe subscription support

            struct State
            {
                public UnsafeEventHandler<T> Observers;
                public AllocatorManager.AllocatorHandle Allocator;
            }

            private unsafe State* m_State; // TODO Instead we might want this in context to solve problem in Destroy
            
            public unsafe UnsafeSubscription Subscribe([NotNull] Context context, UnsafeDelegate<T> observer)
            {
                if (m_State == null)
                {
                    m_State = context.unsafeContext.Allocate<State>();
                    
                    m_State->Observers = new UnsafeEventHandler<T>();
                    m_State->Allocator = context.unsafeContext.allocator;
                }
                
                m_State->Observers.Add(observer);
                
                return new UnsafeSubscription(
                    new UnsafeDelegate<UnsafeCallback>(&Unsubscribe, m_State), observer.ToCallback());
            }
            
            private static unsafe void Unsubscribe(UnsafeCallback callback, void* state)
            {
                var s = (State*)state;
                s->Observers.Remove(callback);
                if (s->Observers.GetInvocationList().Length == 0) // TODO Probably better to use a separate ref count
                    Destroy(s);
            }
            
            private static unsafe void Destroy(State* state)
            {
                state->Observers.Dispose();
                AllocatorManager.Free(state->Allocator, state, sizeof(State), 
                    UnsafeUtility.AlignOf<State>());
                // TODO Not good, would leave dangling state in managed class. Might need additional work
            }
            
            #endregion

            public override void Dispose()
            {
                for (var i = 0; i < m_ObserverCount; ++i)
                {
                    Debug.LogWarning("Subscription not disposed: " + m_Observers[i]);
                }

                unsafe
                {
                    if (m_State != null)
                    {
                        Destroy(m_State);
                        m_State = null;
                    }
                }
                
                //m_Stream?.Dispose(); // TODO Should really ref count
            }

            public bool Equals(IDependencyGraphNode other)
            {
                throw new NotImplementedException();
            }
            
            public NativeSlice<T>.Enumerator GetEnumerator()
            {
                return m_Stream?.GetEnumerator() ?? new NativeSlice<T>.Enumerator();
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
