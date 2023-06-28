#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A caching enumerator that will save all enumerated values on the first iteration, and when
    /// subsequently iterated, return the saved values instead.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ViewStateCollection<T> : IViewStateCollection, IEnumerable<T>
    {
        private readonly IEnumerable<T> m_Collection;
        private readonly IEqualityComparer<T> m_Comparer;
        private IList<T> m_CachedCollection;

        public ViewStateCollection(IEnumerable<T> collection, IEqualityComparer<T> comparer = null)
        {
            m_Collection = collection;
            m_Comparer = comparer;
        }

        public bool SequenceEqual(IViewStateCollection other)
        {
            return other is ViewStateCollection<T> otherCollection && this.SequenceEqual(otherCollection, m_Comparer);
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (m_CachedCollection == null)
            {
                m_CachedCollection = new List<T>();
                using (var enumerator = m_Collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        m_CachedCollection.Add(enumerator.Current);
                    }
                }
            }

            using (var enumerator = m_CachedCollection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

#endif
