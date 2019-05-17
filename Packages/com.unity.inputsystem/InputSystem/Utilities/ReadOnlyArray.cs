using System;
using System.Collections;
using System.Collections.Generic;

////REVIEW: switch to something that doesn't require the backing store to be an actual array?
////  (maybe switch m_Array to an InlinedArray and extend InlinedArray to allow having three configs:
////  1. firstValue only, 2. firstValue + additionalValues, 3. everything in additionalValues)

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Read-only access to an array or to a slice of an array.
    /// </summary>
    /// <typeparam name="TValue">Type of values stored in the array.</typeparam>
    /// <remarks>
    /// The purpose of this struct is to allow exposing internal arrays directly such that no
    /// boxing and no going through interfaces is required but at the same time not allowing
    /// the internal arrays to be modified.
    /// </remarks>
    public struct ReadOnlyArray<TValue> : IReadOnlyList<TValue>
    {
        internal TValue[] m_Array;
        internal int m_StartIndex;
        internal int m_Length;

        /// <summary>
        /// Construct a read-only array covering all of the given array.
        /// </summary>
        /// <param name="array">Array to index.</param>
        public ReadOnlyArray(TValue[] array)
        {
            m_Array = array;
            m_StartIndex = 0;
            m_Length = array?.Length ?? 0;
        }

        /// <summary>
        /// Construct a read-only array that covers only the given slice of <paramref name="array"/>.
        /// </summary>
        /// <param name="array">Array to index.</param>
        /// <param name="index">Index at which to start indexing <paramref name="array"/>. The given element
        /// becomes index #0 for the read-only array.</param>
        /// <param name="length">Length of the slice to index from <paramref name="array"/>.</param>
        public ReadOnlyArray(TValue[] array, int index, int length)
        {
            m_Array = array;
            m_StartIndex = index;
            m_Length = length;
        }

        /// <summary>
        /// Convert to array.
        /// </summary>
        /// <returns>A new array containing a copy of the contents of the read-only array.</returns>
        public TValue[] ToArray()
        {
            var result = new TValue[m_Length];
            if (m_Length > 0)
                Array.Copy(m_Array, m_StartIndex, result, 0, m_Length);
            return result;
        }

        public int IndexOf(Predicate<TValue> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (var i = 0; i < m_Length; ++i)
                if (predicate(m_Array[m_StartIndex + i]))
                    return i;

            return -1;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return new Enumerator<TValue>(m_Array, m_StartIndex, m_Length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator ReadOnlyArray<TValue>(TValue[] array)
        {
            return new ReadOnlyArray<TValue>(array);
        }

        /// <summary>
        /// Number of elements in the array.
        /// </summary>
        public int Count => m_Length;

        /// <summary>
        /// Return the element at the given index.
        /// </summary>
        /// <param name="index">Index into the array.</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public TValue this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length)
                    throw new IndexOutOfRangeException();
                // We allow array to be null as we are patching up ReadOnlyArrays in a separate
                // path in several places.
                if (m_Array == null)
                    throw new InvalidOperationException();
                return m_Array[m_StartIndex + index];
            }
        }

        internal class Enumerator<T> : IEnumerator<T>
        {
            private readonly T[] m_Array;
            private readonly int m_IndexStart;
            private readonly int m_IndexEnd;
            private int m_Index;

            public Enumerator(T[] array, int index, int length)
            {
                m_Array = array;
                m_IndexStart = index - 1; // First call to MoveNext() moves us to first valid index.
                m_IndexEnd = index + length;
                m_Index = m_IndexStart;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (m_Index < m_IndexEnd)
                    ++m_Index;
                return (m_Index != m_IndexEnd);
            }

            public void Reset()
            {
                m_Index = m_IndexStart;
            }

            public T Current
            {
                get
                {
                    if (m_Index == m_IndexEnd)
                        throw new IndexOutOfRangeException();
                    return m_Array[m_Index];
                }
            }

            object IEnumerator.Current => Current;
        }
    }

    public static class ReadOnlyArrayExtensions
    {
        public static bool Contains<TValue>(this ReadOnlyArray<TValue> array, TValue value)
            where TValue : IComparable<TValue>
        {
            for (var i = 0; i < array.m_Length; ++i)
                if (array.m_Array[array.m_StartIndex + i].CompareTo(value) == 0)
                    return true;
            return false;
        }

        public static bool ContainsReference<TValue>(this ReadOnlyArray<TValue> array, TValue value)
            where TValue : class
        {
            return IndexOfReference(array, value) != -1;
        }

        public static int IndexOfReference<TValue>(this ReadOnlyArray<TValue> array, TValue value)
            where TValue : class
        {
            for (var i = 0; i < array.m_Length; ++i)
                if (ReferenceEquals(array.m_Array[array.m_StartIndex + i], value))
                    return i;
            return -1;
        }
    }
}
