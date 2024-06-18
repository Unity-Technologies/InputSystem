using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Implement a generalized pooled version or at least solve subscriptions
    internal class ObjectManager<T>
    {
        private T[] m_Items;
        private int m_Count;

        public ObjectManager(int initialCapacity)
        {
            m_Items = new T[initialCapacity];
            m_Count = 0;
        }

        public int Count => m_Count;

        public ref T GetValue(int index)
        {
            if (index > m_Count) throw new ArgumentOutOfRangeException(nameof(index));
            return ref m_Items[index];
        }
        
        public ref T this[int key] => ref GetValue(key);

        public void Release(int handle)
        {
            
        }
    }
}