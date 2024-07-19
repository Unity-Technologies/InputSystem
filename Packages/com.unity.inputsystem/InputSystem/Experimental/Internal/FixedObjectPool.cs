using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A trivial fixed size pool with best-fit search and free-list defragmentation.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public struct FixedObjectPool<T> 
    {
        private struct Chunk
        {
            public int Offset;
            public int Length;
        }
        
        private readonly T[] m_Array;
        private readonly Chunk[] m_FreeList;
        private int m_Count;

        /// <summary>
        /// Constructs a fixed sized object pool.
        /// </summary>
        /// <param name="size">The size of the pool. Must be greater than zero.</param>
        public FixedObjectPool(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            
            m_Array = new T[size];
            m_FreeList = new Chunk[size];
            m_FreeList[0] = new Chunk { Offset = 0, Length = size };
            m_Count = 1;
        }

        /// <summary>
        /// Rents <paramref name="count"/> consecutive objects from the underlying pool.
        /// </summary>
        /// <param name="count">The number of consecutive objects to be retrieved.</param>
        /// <returns></returns>
        /// <exception cref="Exception">If unable to perform the allocation.</exception>
        public ArraySegment<T> Rent(int count)
        {
            for (var i = 0; i < m_Count; ++i)
            {
                if (m_FreeList[i].Length >= count)
                {
                    var offset = m_FreeList[i].Offset;
                    m_FreeList[i].Offset += count;
                    m_FreeList[i].Length -= count;
                    return new ArraySegment<T>(m_Array, offset, count);
                }
            }

            throw new Exception();
        }

        /// <summary>
        /// Returns an array segment previously rented to the pool.
        /// </summary>
        /// <param name="segment">The segment to be returned.</param>
        /// <exception cref="ArgumentException">If the segment was not rented by this pool.</exception>
        public void Return(ArraySegment<T> segment)
        {
            if (segment.Array != m_Array)
                throw new ArgumentException($"{nameof(segment)} is not part of this pool");

            for (var i = 0; i < m_Count; ++i)
            {
                // Continue scanning for insertion point
                if (m_FreeList[i].Offset <= segment.Offset) 
                    continue;

                // Merge left
                var merged = false;
                var prevIndex = i - 1;
                if (prevIndex >= 0)
                {
                    ref var prev = ref m_FreeList[prevIndex];
                    if (prev.Offset + prev.Length == segment.Offset)
                    {
                        prev.Length += segment.Count;
                        merged = true;
                    }
                }

                // Merge right
                ref var next = ref m_FreeList[i];
                if (segment.Offset + segment.Count == next.Offset)
                {
                    if (merged)
                    {
                        m_FreeList[prevIndex].Length += next.Length;
                        Array.Copy(m_FreeList, i+1, m_FreeList, i, m_Count - i - 1);
                        --m_Count;
                    }
                    else
                    {
                        next.Length += segment.Count;
                        next.Offset = segment.Offset;    
                    }
                    return;
                }
                
                if (merged) 
                    return;

                // Insert since left and right is not adjacent
                Array.Copy(m_FreeList, i, m_FreeList, i + 1, m_Count - i);
                m_FreeList[i] = new Chunk() { Offset = segment.Offset, Length = segment.Count };
                ++m_Count;
                return;
            }

            // Insert at end
            m_FreeList[m_Count++] = new Chunk() { Offset = segment.Offset, Length = segment.Count };
        }
    }
}