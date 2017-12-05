using System;
using System.Collections;
using System.Collections.Generic;
#if !NET_4_0
using ISX.Net35Compatibility;
#endif

namespace ISX.Utilities
{
    public struct ReadOnlyArray<TValue> : IReadOnlyList<TValue>
    {
        internal TValue[] m_Array;
        internal int m_StartIndex;
        internal int m_Length;

        public ReadOnlyArray(TValue[] array)
        {
            m_Array = array;
            m_StartIndex = 0;
            if (array != null)
                m_Length = array.Length;
            else
                m_Length = 0;
        }

        public ReadOnlyArray(TValue[] array, int index, int length)
        {
            m_Array = array;
            m_StartIndex = index;
            m_Length = length;
        }

        public TValue[] ToArray()
        {
            if (m_Length == 0)
                return null;
            var result = new TValue[m_Length];
            Array.Copy(m_Array, m_StartIndex, result, 0, m_Length);
            return result;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return new Enumerator<TValue>(m_Array, m_StartIndex, m_Length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return m_Length; }
        }

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
            private T[] m_Array;
            private int m_Index;
            private int m_IndexStart;
            private int m_IndexEnd;

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

            object IEnumerator.Current
            {
                get { return Current; }
            }
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
    }
}
