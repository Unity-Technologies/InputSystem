using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Implement, an observable list. Use for things like devices.
    public class ObservableList<T> 
        : ICollection<T>, IList<T>, IEnumerable<T>, IEnumerable
    {
        private List<T> m_List;

        public void Add(T item)
        {
            m_List.Add(item);
            // TODO Trigger Add
        }

        public void Clear()
        {
            m_List.Clear();
            // TODO Trigger Remove
        }
        
        public bool Remove(T item)
        {
            var result = m_List.Remove(item);
            if (result)
            {
                // TODO Trigger Remove
            }
            return result;
        }
        
        public void Insert(int index, T item) 
        {
            m_List.Insert(index, item);
            // TODO Trigger Add
        }

        public void RemoveAt(int index)
        {
            m_List.RemoveAt(index);
            // TODO Trigger Remove
        }

        public T this[int index]
        {
            get => m_List[index];
            set
            {
                m_List[index] = value;
                // TODO Trigger Change
            }
        }

        public IEnumerator<T> GetEnumerator() => m_List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(T item) => m_List.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => m_List.CopyTo(array, arrayIndex);
        public int Count => m_List.Count;
        public bool IsReadOnly => false; // TODO What (private)?
        public int IndexOf(T item) => m_List.IndexOf(item);
    }
}