// TODO We should get rid of this if possible

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tests.InputSystem.Experimental
{
    internal struct UnsafeArray<T> : IDisposable 
        where T : unmanaged
    {
        private unsafe T* m_Data;
        private int m_Capacity;
        private int m_Length;
        private readonly AllocatorManager.AllocatorHandle m_Allocator;

        public unsafe UnsafeArray(AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = null;
            m_Capacity = 0;
            m_Length = 0;
            m_Allocator = allocator;
        }
        
        public unsafe UnsafeArray(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = AllocatorManager.Allocate<T>(allocator, initialCapacity);
            m_Capacity = initialCapacity;
            m_Length = 0;
            m_Allocator = allocator;
        }
        
        public unsafe void Dispose()
        {
            if (m_Data == null) return;
            AllocatorManager.Free(m_Allocator, m_Data);
            m_Data = null;
        }

        public unsafe T* data => m_Data;
        public int capacity => m_Capacity;
        public int length => m_Length;
        
        public unsafe ref T this[int key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *(m_Data + key);
        }

        public unsafe void RemoveAt(int index)
        {
            var dst = m_Data + index;
            UnsafeUtility.MemMove(dst, dst + 1, m_Length - index);
            --m_Length;
        }

        public void Resize(int newSize)
        {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));
            
            if (newSize > m_Length)
            {
                Reserve(newSize);
                m_Length = newSize;
            }
            else if (newSize < m_Length)
            {
                m_Length = newSize;
            }
        }
        
        public unsafe void Reserve(int newCapacity)
        {
            var newPtr = AllocatorManager.Allocate<T>(m_Allocator, newCapacity);
            if (m_Data != null)
            {
                var newSize = Math.Min(m_Length, newCapacity);
                UnsafeUtility.MemCpy(newPtr, m_Data, newSize);
                AllocatorManager.Free(m_Allocator, m_Data);
            }
            
            m_Data = newPtr;
            m_Capacity = newCapacity;
        }
    }
}