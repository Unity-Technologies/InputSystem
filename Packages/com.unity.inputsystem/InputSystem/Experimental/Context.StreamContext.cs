using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

        // TODO Implement IAsyncEnumerable<T>?
        internal sealed class StreamContext<T> : StreamContext, IObservable<T>, IDisposable, IEnumerable<T>
            where T : struct
        {
            private readonly Usage m_Usage;         // The associated usage
            private IObserver<T>[] m_Observers;     // The associated observers of this stream
            private int m_ObserverCount;            // The current observer count
            private Stream<T> m_Stream;             // Reference to associated underlying stream (if any)

            public StreamContext(Usage usage, Stream<T> stream = null)
                : base()
            {
                m_Usage = usage;
                m_Stream = stream;
            }

            //public Stream<T> stream => Stream as Stream<T>;

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

            public IDisposable Subscribe(IObserver<T> observer)
            {
                ArrayHelpers.AppendWithCapacity<IObserver<T>>(ref m_Observers, ref m_ObserverCount, observer);
                return new Subscription(this, observer);
            }

            public override void Dispose()
            {
                for (var i = 0; i < m_ObserverCount; ++i)
                {
                    Debug.LogWarning("Subscription not disposed: " + m_Observers[i]);
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

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
