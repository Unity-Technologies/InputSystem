using System;
using System.Collections;
using System.Collections.Generic;
#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

namespace UnityEngine.Experimental.Input.Utilities
{
    // Variation of ReadOnlyArray that has the slicing ability but
    // does provide write access. Used only internally.
    internal struct ReadWriteArray<TValue> : IReadOnlyList<TValue>
    {
        internal TValue[] m_Array;
        internal int m_StartIndex;
        internal int m_Length;

        public ReadWriteArray(TValue[] array)
        {
            m_Array = array;
            m_StartIndex = 0;
            if (array != null)
                m_Length = array.Length;
            else
                m_Length = 0;
        }

        public ReadWriteArray(TValue[] array, int index, int length)
        {
            m_Array = array;
            m_StartIndex = index;
            m_Length = length;
        }

        public ReadOnlyArray<TValue> AsReadOnly()
        {
            return new ReadOnlyArray<TValue>(m_Array, m_StartIndex, m_Length);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return new ReadOnlyArray<TValue>.Enumerator<TValue>(m_Array, m_StartIndex, m_Length);
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
                // We allow array to be null as we are patching up ReadWriteArrays in a separate
                // path in several places.
                if (m_Array == null)
                    throw new InvalidOperationException();
                return m_Array[m_StartIndex + index];
            }
            set
            {
                if (index < 0 || index >= m_Length)
                    throw new IndexOutOfRangeException();
                if (m_Array == null)
                    throw new InvalidOperationException();
                m_Array[m_StartIndex + index] = value;
            }
        }
    }
}
