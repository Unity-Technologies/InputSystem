using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.InputSystem.Utilities;

// TODO This PoC doesn't contain any native stream at the moment which would exist with a real backend.

namespace UnityEngine.InputSystem.Experimental
{
    public interface IStream : IDisposable
    {
        // TODO Should provide type safety conversions and checks, potentially observable
    }



    // TODO When writing a device to streams, we want the device representation to be written to one stream
    //      and individual controls to be written to control streams. We may pack data that is used together
    //      together.
    
    // Rewrite this to be a unmanaged type allocated in native memory, currently only a hackish variant
    // to conceptualize higher order logic. If binary compatible with an opaque counterpart we may
    // cast between them and drop the interface to avoid boxing.a
    
    // TODO Write two stream implementations, one local stream and once shared stream.

    public static class StreamOperations
    {
        public static void Press(Stream<InputEvent> dst, Stream<bool> src)
        {
            var span = src.AsExtendedSpan();
            for (int i = 1, j = 0; i < span.Length; ++i, ++j)
            {
                if (span[i] != span[j] && !span[j])
                    dst.OfferByValue(new InputEvent());
            }
        }
        
        public static void Multiplex<T>(Stream<T> first, Stream<T> source2) where T : struct
        {
            // TODO Here we actually need to utilize timestamp to multiplex compared to callbacks
            // TODO Fetch timeline:
            // TODO - Use sample id to order samples since global
        }
        
        // TODO public static void Multipex<T>(Stream<Packet>)
    }
    
    /// <summary>
    /// A stream containing objects of type T.
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    /// <remarks>
    /// Some specific properties of this container regarding this implementation that makes it specialized:
    /// - The stream may consist of [1, n] linked contiguous segments.
    /// - The span (including its length) of a stream reflects all reported samples for the related time view.
    /// - A stream requires an initial value when created, except if it represents an event stream, then there
    ///   is no initial value.
    /// - A stream offers a reference to the last sampled or received value of the stream even if the span is empty.
    /// - The last sampled value is stored at index -1.
    /// </remarks>
    public class Stream<T> : IStream, IEnumerable<T> where T : struct
    {
        // TODO Rewrite properly, currently just to provide higher level concepts
        
        private NativeArray<T> m_Values;
        private int m_Count;
        public readonly Usage Usage;

        public const int kDefaultCapacity = 2;

        public Stream(Usage usage, T initialValue, int initialCapacity = kDefaultCapacity)
            : this(usage, ref initialValue, initialCapacity)
        {
        }
        
        public Stream(Usage usage, ref T initialValue, int initialCapacity = kDefaultCapacity)
        {
            Usage = usage;
            m_Values = new NativeArray<T>(initialCapacity, Allocator.Persistent);

            // An initial value is required for any absolute state stream but its not accounted for in the list
            // of streamed values.
            m_Values[0] = initialValue;
            m_Count = 1;
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return m_Values.GetSubArray(1, m_Count - 1).AsReadOnlySpan();
        }

        public ReadOnlySpan<T> AsExtendedSpan()
        {
            return m_Values.GetSubArray(0, m_Count).AsReadOnlySpan();
        }

        public void Advance() // TODO Temporary
        {
            if (m_Count <= 1)
                return;

            m_Values[0] = m_Values[m_Count-1];
            m_Count = 1;
        }

        public void OfferByRef(ref T value)
        {
            if (m_Values.Length == m_Count)
                Reallocate(m_Values.Length + 1);
            m_Values[m_Count++] = value;
        }

        public void OfferByValue(T value)
        {
            if (m_Values.Length == m_Count)
                Reallocate(m_Values.Length + 1);
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

        public T Previous => m_Values[0];
        public T GetLast() => m_Values[m_Count - 1];

        //public static implicit operator ReadOnlySpan<T>(Stream<T> stream) => stream.AsSpan();

        public void Dispose()
        {
            m_Values.Dispose();
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return m_Values.Slice(1, m_Count - 1).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
