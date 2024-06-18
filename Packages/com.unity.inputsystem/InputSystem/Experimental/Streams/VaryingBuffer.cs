using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    public unsafe struct VaryingBuffer : IDisposable
    {
        private struct Segment
        {
            public void* First;
            public void* Last;
            public void* Capacity;
            public Segment* Next;
        }

        private Segment* m_Head;
        private Segment* m_Current;
        private readonly AllocatorManager.AllocatorHandle m_Allocator;
        
        public VaryingBuffer(int initialSegmentSize, AllocatorManager.AllocatorHandle allocator)
        {
            m_Allocator = allocator;
            m_Head = AllocateSegment(initialSegmentSize, allocator);
            m_Current = m_Head;
        }

        private static Segment* AllocateSegment(int sizeBytes, AllocatorManager.AllocatorHandle allocator)
        {
            var headerSizeBytes = UnsafeUtility.SizeOf<Segment>();
            var alignment = UnsafeUtility.AlignOf<Segment>();
            var ptr = (Segment*)allocator.Allocate(sizeBytes, alignment, 1);
            ptr->First = OffsetPtr(ptr, headerSizeBytes);
            ptr->Last = OffsetPtr(ptr, sizeBytes);
            ptr->Next = null;
            return ptr;
        }

        private static void DeallocateSegment(Segment* segment, AllocatorManager.AllocatorHandle allocator)
        {
            AllocatorManager.Free(allocator, segment);
        }

        public void Push(void* data, int size, int align)
        {
            var dst = Align(m_Current->Last, align);
            if (dst < m_Current->Capacity)
            {
                UnsafeUtility.MemCpy(dst, data, size);
                m_Current->Last = OffsetPtr(dst, size);
            }
            else
            {
                var oldSizeBytes = (ulong)m_Current->Capacity - (ulong)m_Current;
                var newSizeBytes = oldSizeBytes * 2;
                // TODO Need to take requested element size into account
                var newSegment = AllocateSegment((int)newSizeBytes, m_Allocator);
                m_Current->Next = newSegment;
                m_Current = newSegment;

                dst = Align(newSegment->Last, align);
                UnsafeUtility.MemCpy(dst, data, size);
                m_Current->Last = OffsetPtr(dst, size);
            }
        }

        public unsafe void Push<T>(ref T data) where T : unmanaged
        {
            Push(UnsafeUtility.AddressOf(ref data), UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>());
        }

        public void Push<T>(T data) where T : unmanaged
        {
            Push(ref data);
        }

        public void Clear()
        {
            while (m_Head != m_Current)
            {
                var next = m_Head->Next;
                DeallocateSegment(m_Head, m_Allocator);
                m_Head = next;
            }
        }

        public void Dispose()
        {
            var ptr = m_Head;
            while (ptr != null)
            {
                var next = ptr->Next;
                DeallocateSegment(ptr, m_Allocator);
                ptr = next;
            }
            m_Head = null;
        }

        private static unsafe void* OffsetPtr(void* ptr, int offsetBytes)
        {
            return ((byte*)ptr) + offsetBytes;
        }

        private static unsafe void* Align(void* ptr, int alignment)
        {
            return (void*)CollectionHelper.Align((ulong)ptr, (ulong)alignment);
        }
        
        // TODO Get direct access to buffer pointer (or writer).
        // TODO Enable forward iterator.
    }
}