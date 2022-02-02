using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Helper when having either a single element or a list of elements. Avoids
    /// having to allocate GC heap garbage or having to alternatively split code paths.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    internal struct OneOrMore<TValue, TList> : IReadOnlyList<TValue>
        where TList : IReadOnlyList<TValue>
    {
        private readonly bool m_IsSingle;
        private readonly TValue m_Single;
        private readonly TList m_Multiple;

        public int Count => m_IsSingle ? 1 : m_Multiple.Count;

        public TValue this[int index]
        {
            get
            {
                if (!m_IsSingle)
                    return m_Multiple[index];

                if (index < 0 || index > 1)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return m_Single;
            }
        }

        public OneOrMore(TValue single)
        {
            m_IsSingle = true;
            m_Single = single;
            m_Multiple = default;
        }

        public OneOrMore(TList multiple)
        {
            m_IsSingle = false;
            m_Single = default;
            m_Multiple = multiple;
        }

        public static implicit operator OneOrMore<TValue, TList>(TValue single)
        {
            return new OneOrMore<TValue, TList>(single);
        }

        public static implicit operator OneOrMore<TValue, TList>(TList multiple)
        {
            return new OneOrMore<TValue, TList>(multiple);
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return new Enumerator { m_List = this };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<TValue>
        {
            internal int m_Index = -1;
            internal OneOrMore<TValue, TList> m_List;

            public bool MoveNext()
            {
                ++m_Index;
                if (m_Index >= m_List.Count)
                    return false;
                return true;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public TValue Current => m_List[m_Index];
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}
