using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Helper when having either a single element or a list of elements. Avoids
    /// having to allocate GC heap garbage or having to alternatively split code paths.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    internal struct OneOrMore<TValue, TList>
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
    }
}
