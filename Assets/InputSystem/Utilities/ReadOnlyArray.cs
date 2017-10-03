using System;
using System.Collections;
using System.Collections.Generic;

namespace ISX
{
    // Read-only access to an array. Additionally allows to expose only
    // a slice of the whole array.
    // NOTE: Use indexer instead of enumerator if you want to avoid garbage.
    public struct ReadOnlyArray<TValue> : IReadOnlyList<TValue>
    {
        private TValue[] m_Array;
        private int m_StartIndex;
        private int m_Length;

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
                return m_Array[m_StartIndex + index];
            }
        }

        private class Enumerator<T> : IEnumerator<T>
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
}
