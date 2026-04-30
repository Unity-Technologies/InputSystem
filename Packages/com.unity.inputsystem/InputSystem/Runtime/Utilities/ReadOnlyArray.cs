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
    ///
    /// It differs from <c>ReadOnlySpan&lt;T&gt;</c> in that it can be stored on the heap and differs
    /// from <c>ReadOnlyCollection&lt;T&gt;</c> in that it supports slices directly without needing
    /// an intermediate object representing the slice.
    ///
    /// Note that in most cases, the ReadOnlyArray instance should be treated as a <em>temporary</em>.
    /// The actual array referred to by a ReadOnlyArray instance is usually owned and probably mutated
    /// by another piece of code. When that code makes changes to the array, the ReadOnlyArray
    /// instance will not get updated.
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

        /// <summary>
        /// Searches for the first element in the array for which the given <paramref name="predicate"/> is <c>true</c> and returns the index of that element.
        /// </summary>
        /// <param name="predicate">The predicate to be evaluated for each element which defines the condition for the search.</param>
        /// <returns>Index of the first element for which <paramref name="predicate"/> is <c>true</c>x or -1 if no such element exists.</returns>
        /// <exception cref="ArgumentNullException">If predicate is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// // Searches for the first element in an integer array that is greater or equal to 5.
        /// var haystack = new ReadOnlyArray&lt;int&gt;(new[] { 1, 2, 3, 4, 5, 6, 7 });
        /// var index = haystack.IndexOf((value) => value >= 5); // index == 4
        /// </code>
        /// </example>
        public int IndexOf(Predicate<TValue> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (var i = 0; i < m_Length; ++i)
                if (predicate(m_Array[m_StartIndex + i]))
                    return i;

            return -1;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the read-only array.
        /// <returns>
        /// <see cref="ReadOnlyArray{TValue}.Enumerator"/>
        /// An enumerator for the read-only array.
        /// </returns>
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_Array, m_StartIndex, m_Length);
        }

        /// <inheritdoc/>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Constructs a read-only array containing elements <paramref name="array"/>.
        /// </summary>
        /// <param name="array">An existing array containing elements to be wrapped as a read-only array.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "`ToXXX` message only really makes sense as static, which is not recommended for generic types.")]
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
                    throw new ArgumentOutOfRangeException(nameof(index));
                // We allow array to be null as we are patching up ReadOnlyArrays in a separate
                // path in several places.
                if (m_Array == null)
                    throw new InvalidOperationException();
                return m_Array[m_StartIndex + index];
            }
        }

        /// <summary>
        /// <see cref="ReadOnlyArray{TValue}"/>
        /// Enumerates the elements of a read-only array.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly TValue[] m_Array;
            private readonly int m_IndexStart;
            private readonly int m_IndexEnd;
            private int m_Index;

            internal Enumerator(TValue[] array, int index, int length)
            {
                m_Array = array;
                m_IndexStart = index - 1; // First call to MoveNext() moves us to first valid index.
                m_IndexEnd = index + length;
                m_Index = m_IndexStart;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (m_Index < m_IndexEnd)
                    ++m_Index;
                return m_Index != m_IndexEnd;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                m_Index = m_IndexStart;
            }

            /// <inheritdoc/>
            public TValue Current
            {
                get
                {
                    if (m_Index == m_IndexEnd)
                        throw new InvalidOperationException("Iterated beyond end");
                    return m_Array[m_Index];
                }
            }

            object IEnumerator.Current => Current;
        }
    }

    /// <summary>
    /// Extension methods to help with <see cref="ReadOnlyArrayExtensions"/> contents.
    /// </summary>
    public static class ReadOnlyArrayExtensions
    {
        /// <summary>
        /// Evaluates whether <paramref name="array"/> contains an element that compares equal to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TValue">The array element type.</typeparam>
        /// <param name="array">Reference to the read-only array to be searched.</param>
        /// <param name="value">The value to be searched for in <paramref name="array"/>.</param>
        /// <returns><c>true</c> if <paramref name="array"/> contains <paramref name="value"/>, else <c>false</c>.</returns>
        public static bool Contains<TValue>(this ReadOnlyArray<TValue> array, TValue value)
            where TValue : IComparable<TValue>
        {
            for (var i = 0; i < array.m_Length; ++i)
                if (array.m_Array[array.m_StartIndex + i].CompareTo(value) == 0)
                    return true;
            return false;
        }

        /// <summary>
        /// Evaluates whether <paramref name="array"/> contains a reference to <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TValue">The array element type.</typeparam>
        /// <param name="array">Reference to the read-only array to be searched.</param>
        /// <param name="value">The reference to be searched for in <paramref name="array"/>.</param>
        /// <returns><c>true</c> if <paramref name="array"/> contains a reference to <paramref name="value"/>, else <c>false</c>.</returns>
        public static bool ContainsReference<TValue>(this ReadOnlyArray<TValue> array, TValue value)
            where TValue : class
        {
            return IndexOfReference(array, value) != -1;
        }

        /// <summary>
        /// Retrieves the index of <paramref name="value"/> in <paramref name="array"/>.
        /// </summary>
        /// <typeparam name="TValue">The array element type.</typeparam>
        /// <param name="array">Reference to the read-only array to be searched.</param>
        /// <param name="value">The reference to be searched for in <paramref name="array"/>.</param>
        /// <returns>The zero-based index of element <paramref name="value"/> in <paramref name="array"/> if such a reference could be found and -1 if it do not exist.</returns>
        public static int IndexOfReference<TValue>(this ReadOnlyArray<TValue> array, TValue value)
            where TValue : class
        {
            for (var i = 0; i < array.m_Length; ++i)
                if (ReferenceEquals(array.m_Array[array.m_StartIndex + i], value))
                    return i;
            return -1;
        }

        /// <summary>
        /// Evaluates whether whether <paramref name="array1"/> and <paramref name="array2"/> contain the same sequence of references.
        /// </summary>
        /// <typeparam name="TValue">The array element type.</typeparam>
        /// <param name="array1">The first array to be evaluated.</param>
        /// <param name="array2">The second array to be evaluated.</param>
        /// <param name="count">The maximum number of elements to be compared.</param>
        /// <returns><c>true</c> if the <paramref name="count"/> first elements of <paramref name="array1"/> and <paramref name="array2"/> contain the same references, else false.</returns>
        internal static bool HaveEqualReferences<TValue>(this ReadOnlyArray<TValue> array1, IReadOnlyList<TValue> array2, int count = int.MaxValue)
        {
            var length1 = Math.Min(array1.Count, count);
            var length2 = Math.Min(array2.Count, count);

            if (length1 != length2)
                return false;

            for (var i = 0; i < length1; ++i)
                if (!ReferenceEquals(array1[i], array2[i]))
                    return false;

            return true;
        }
    }
}
