using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using UnityEngine.InputSystem.Utilities;

// TODO This PoC doesn't contain any native stream at the moment which would exist with a real backend.

namespace UnityEngine.InputSystem.Experimental
{
    
    public interface IStream : IDisposable
    {
        // TODO Should provide type safety conversions and checks, potentially observable
    }

    // Rewrite this to be a unmanaged type allocated in native memory, currently only a hackish variant
    // to conceptualize higher order logic. If binary compatible with an opaque counterpart we may
    // cast between them and drop the interface to avoid boxing.
    public class Stream<T> : IStream where T : struct
    {
        private NativeArray<T> m_Values;
        private int m_Count;
        public readonly Usage Usage;
        
        public Stream(Usage usage, ref T value, int initialCapacity = 2)
        {
            Usage = usage;
            m_Values = new NativeArray<T>(initialCapacity, Allocator.Persistent);
            
            // An initial value is required for any absolute state stream
            m_Values[0] = value;
            m_Count = 1;
        }
        
        public ReadOnlySpan<T> AsSpan()
        {
            //return m_Values.AsReadOnlySpan()[..m_Count];
            return m_Values.GetSubArray(0, m_Count).AsReadOnlySpan();
        }

        public void OfferByRef(ref T value)
        {
            if (m_Values.Length == m_Count)
                Reallocate(m_Values.Length+1);
            m_Values[m_Count++] = value;
        }

        public void OfferByValue(T value)
        {
            if (m_Values.Length == m_Count)
                Reallocate(m_Values.Length+1);
            m_Values[m_Count++] = value;
        }

        private void Reallocate(int newCapacity)
        {
            // TODO Check newCapacity is greater
            var newArray = new NativeArray<T>(newCapacity, Allocator.Persistent);
            for (var i = 0; i < m_Count; ++i)
                newArray[i] = m_Values[i];
            m_Values.Dispose();
            m_Values = newArray;
        }

        public static implicit operator ReadOnlySpan<T>(Stream<T> stream) => stream.AsSpan();

        public void Dispose()
        {
            m_Values.Dispose();
        }
    }
    
    // TODO Might not be we need this... but needs some registration with context.
    // TODO If we anyway wrap this maybe it should be a class?
    // public class ObservableStream<T> : IStream, IObservable<T> 
    //     where T : struct
    // {
    //     public readonly Usage usage;
    //     private Stream<T> m_Stream; // TODO Bad, copy, also bad since we want it segmented different representations at reader and writer 
    //     private IObserver<T>[] m_Observers;
    //     private int m_ObserverCount;
    //     
    //     public ObservableStream(Usage usage)
    //     {
    //         this.usage = usage;
    //         m_Stream = null;
    //         m_Observers = null;
    //         m_ObserverCount = 0;
    //     }
    //
    //     public void Process()
    //     {
    //         if (m_Stream == null)
    //             return;
    //         
    //         var span = m_Stream.AsSpan();
    //         for (var i = 0; i < span.Length; ++i)
    //         {
    //             for (var j = 0; j < m_ObserverCount; ++j)
    //             {
    //                 m_Observers[j].OnNext(span[i]);
    //             }
    //             // TODO Call OnComplete
    //         }
    //     }
    //
    //     // Represents a subscription and allows unsubscription
    //     private sealed class Subscription : IDisposable
    //     {
    //         private ObservableStream<T> m_Stream;
    //         private IObserver<T> m_Observer;
    //         
    //         public Subscription(ObservableStream<T> stream, IObserver<T> observer)
    //         {
    //             m_Stream = stream;
    //             m_Observer = observer;
    //         }
    //         
    //         public void Dispose()
    //         {
    //             ArrayHelpers.Erase(ref m_Stream.m_Observers, m_Observer);
    //             --m_Stream.m_ObserverCount;
    //         }
    //     }
    //     
    //     #region IObservable interface
    //     
    //     public IDisposable Subscribe([NotNull] IObserver<T> observer)
    //     {
    //         // Preconditions
    //         if (observer == null)
    //             throw new ArgumentNullException(nameof(observer));
    //         
    //         // Register observer of stream (Note that the underlying stream may not exist)
    //         ArrayHelpers.AppendWithCapacity(ref m_Observers, ref m_ObserverCount, observer, 1);
    //         return new Subscription(this, observer);
    //     }
    //     
    //     #endregion
    // }
}