using System;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    public partial class Context
    {
        internal abstract class StreamContext : IDisposable
        {
            public abstract void Advance(); // TEMP
            public abstract void Process();
            public abstract void Dispose();
        }

        internal sealed class StreamContext<T> : StreamContext, IObservable<T>, IDisposable where T : struct
        {
            private readonly Usage m_Usage;
            private IObserver<T>[] m_Observers;
            private int m_ObserverCount;
            private Stream<T> m_Stream; // not owned by this context

            public StreamContext(Usage usage, Stream<T> stream = null)
                : base()
            {
                m_Usage = usage;
                m_Stream = stream;
            }

            //public Stream<T> stream => Stream as Stream<T>;

            public void SetStream(Stream<T> stream)
            {
                m_Stream = stream;
            }

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
                var span = m_Stream.AsSpan();
                for (var i = 0; i < span.Length; ++i)
                {
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
        }
    }
}
