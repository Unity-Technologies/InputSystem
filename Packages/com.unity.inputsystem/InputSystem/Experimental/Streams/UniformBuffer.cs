using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// TODO Add safety checks
// TODO Make compliant with Job system and burst
// TODO Implement readonly variant
// TODO Benchmark copying value vs dereferencing pointer in enumerator
// TODO Find a good way to expose enumeration over segments as ReadOnlySpan<T>

// TODO Test with TypeEncoded

// TODO FIX Some kind of access violation currently happening

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a fixed-size segment of consecutive memory to hold values of <typeparamref name="T"/> that is
    /// optionally part of an intrusive linked list of segments.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal unsafe struct LinkedSegment<T> where T : unmanaged
    {
        /// <summary>
        /// Points to the first element of the consecutive segment buffer.
        /// </summary>
        public T* Ptr;
        
        /// <summary>
        /// Points to the next segment in the linked list of available segments if any.
        /// </summary>
        public LinkedSegment<T>* Next;
        
        /// <summary>
        /// The current length of the segment in number of elements currently stored.
        /// </summary>
        public int Length;
        
        /// <summary>
        /// The maximum capacity of the segment in number of elements.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// Allocates and initializes a new segment of the given capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the segment in number of elements of type
        /// <typeparam name="T"></typeparam>.</param>
        /// <param name="allocator">The allocator to be use to allocate the segment</param>
        /// <returns>Pointer to allocated segment.</returns>
        public static LinkedSegment<T>* Create(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive");
            
            // Co-allocate segment header and data in the same memory region.
            // Note that we pad segment size based on alignment requirement of type T.
            var headerSizeBytes = CollectionHelper.Align(UnsafeUtility.SizeOf<LinkedSegment<T>>(), UnsafeUtility.AlignOf<T>());
            var itemsSizeBytes = capacity * UnsafeUtility.SizeOf<T>();
            var totalSizeBytes = headerSizeBytes + itemsSizeBytes;
            var ptr = allocator.Allocate(sizeOf: totalSizeBytes, alignOf: UnsafeUtility.AlignOf<LinkedSegment<T>>());
            CheckAlignment(ptr, allocator);
            
            // Compute data pointer
            var dataPtr = (T*)((ulong)ptr) + headerSizeBytes;
            CheckAlignment(dataPtr, allocator);
            
            // Initialize segment 
            var segment = (LinkedSegment<T>*)ptr;
            segment->Ptr = dataPtr;
            segment->Next = null;
            segment->Length = 0;
            segment->Capacity = capacity;
            
            return segment;
        }

        public static void Destroy(LinkedSegment<T>* segment, AllocatorManager.AllocatorHandle allocator)
        {
            AllocatorManager.Free(allocator, segment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return new Span<T>(Ptr, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return new ReadOnlySpan<T>(Ptr, Length);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckAlignment(void* ptr, AllocatorManager.AllocatorHandle allocator)
        {
            if (!CollectionHelper.IsAligned(ptr, UnsafeUtility.AlignOf<LinkedSegment<T>>()))
            {
                AllocatorManager.Free(allocator, ptr);
                throw new ArgumentException("Invalid memory alignment");
            }
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
    }
    
    /// <summary>
    /// A resizeable buffer holding elements of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The buffer utilizes a greedy allocation scheme where data is organized into linked segments in memory.
    /// Hence the buffer is similar to a deque when pressure is high on the container and is similar to a fixed
    /// array when pressure is low and capacity is sufficient.
    /// The buffer automatically allocates new segments with an exponential growth allocation strategy when running
    /// out of capacity but do not deallocate until Clear() is called, in which case all but the last segment
    /// are dropped and deallocated. 
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    public unsafe struct UniformBuffer<T> : IDisposable, IEnumerable<T>
        where T : unmanaged 
    {
        private LinkedSegment<T>* m_Head;
        private LinkedSegment<T>* m_Tail;
        private readonly AllocatorManager.AllocatorHandle m_Allocator;

        public UniformBuffer(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Head = LinkedSegment<T>.Create(capacity, allocator);
            m_Tail = m_Head;
            m_Allocator = allocator;
        }

        public void Push(T value)
        {
            Push(ref value);
        }
        
        public void Push(ref T value)
        {
            CheckCreated();
            
            if (m_Tail->Length >= m_Tail->Capacity)
            {
                var segment = LinkedSegment<T>.Create(m_Tail->Capacity * 2, m_Allocator); 
                m_Tail->Next = segment;
                m_Tail = segment;
            }   
            
            UnsafeUtility.WriteArrayElement(m_Tail->Ptr, m_Tail->Length++, value);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            if ((IntPtr)m_Head == IntPtr.Zero)
                throw new Exception("Container not created");
            if ((IntPtr)m_Tail == IntPtr.Zero)
                throw new Exception("Container not created");

            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }

        public void Clear()
        {
            CheckCreated();
            
            m_Head = Prune(m_Tail);
            m_Head->Next = null;
            m_Head->Length = 0;
        }

        public void Dispose()
        {
            CheckCreated();
            
            m_Head = Prune(null);
            m_Tail = null;
        }

        private LinkedSegment<T>* Prune(LinkedSegment<T>* end)
        {
            for (var current = m_Head; current != end; /* no-op */)
            {
                var temp = current;
                current = current->Next;
                LinkedSegment<T>.Destroy(temp, m_Allocator);
            }
            return end;
        }
        
        public struct Enumerator : IEnumerator<T>
        {
            private readonly UniformBuffer<T> m_Container;
            private LinkedSegment<T> m_Current;
            private int m_Index;
            private T m_Value;
            
            public Enumerator(ref UniformBuffer<T> container)
            {
                m_Container = container;
                m_Current = *m_Container.m_Head;
                m_Index = -1;
                m_Value = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // Attempt to move to next item in current segment
                if (++m_Index < m_Current.Length)
                {
                    //AtomicSafetyHandle.CheckReadAndThrow(this.m_Array.m_Safety);
                    m_Value = UnsafeUtility.ReadArrayElement<T>(m_Current.Ptr, m_Index);
                    return true;
                }
                
                // Move to next segment if such a segment exist 
                if (m_Current.Next != null)
                {
                    m_Current = *m_Current.Next;
                    if (m_Current.Length > 0)
                    {
                        m_Value = *m_Current.Ptr;
                        m_Index = 0;
                        return true;
                    }
                }
                
                // End of enumerable range reached
                m_Value = default (T);
                return false;
            }

            public void Reset()
            {
                m_Index = -1;
                m_Current = *m_Container.m_Head; // copy
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Value;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Value;
            }

            public void Dispose() { }
        }
        
        public Enumerator GetEnumerator() => new (container: ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(container: ref this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}