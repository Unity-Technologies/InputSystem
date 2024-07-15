#define INPUT_SYSTEM_COALLOCATE // TODO Delete this later, currently exists to support comparison

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    internal unsafe struct LinkedSegment
    {
        public void* Ptr;
        public LinkedSegment* Next;
        public int Length;
        public int Capacity;

        public bool Push(void* data, int sizeBytes, int alignOf)
        {
            var dst = UnsafeUtils.OffsetAndAlignUp(Ptr, sizeBytes, alignOf);
            var next = UnsafeUtils.Offset(dst, sizeBytes);
            if (next > Ptr) 
                return false;
            UnsafeUtility.MemCpy(dst, data, sizeBytes);
            return true;
        }
        
        public static LinkedSegment* Create(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException($"Capacity must be positive, but was {capacity}.");

            var segment = UnsafeUtils.AllocateHeaderAndData<LinkedSegment, byte>(allocator, capacity, out var data);
            segment->Ptr = data;
            segment->Next = null;
            segment->Length = 0;
            segment->Capacity = capacity;
            
            return segment;
        }
        
        public static void Destroy(LinkedSegment* segment, AllocatorManager.AllocatorHandle allocator)
        {
#if INPUT_SYSTEM_COALLOCATE
            AllocatorManager.Free(allocator, segment);
#else
            AllocatorManager.Free(allocator, segment->Ptr);
            AllocatorManager.Free(allocator, segment);
#endif // INPUT_SYSTEM_COALLOCATE
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckAlignment(LinkedSegment* ptr, AllocatorManager.AllocatorHandle allocator)
        {
            if (!CollectionHelper.IsAligned(ptr, UnsafeUtility.AlignOf<LinkedSegment>()))
            {
                Destroy(ptr, allocator);
                throw new ArgumentException("Invalid segment memory alignment");
            }
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
    }
    
    /// <summary>
    /// Represents a fixed-size segment of consecutive memory to hold values of <typeparamref name="T"/> that is
    /// optionally part of an intrusive linked list of segments.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// If INPUT_SYSTEM_COALLOCATE is defined, the segment will coallocate header and data region into a single
    /// allocation. If INPUT_SYSTEM_COALLOCATE is not defined, the segment will perform separate allocations for
    /// header and data.
    /// </remarks>
    internal unsafe struct LinkedSegment<T> : IEnumerable<T>
        where T : unmanaged
    {
        /// <summary>
        /// Points to the first element of the consecutive segment buffer data region.
        /// </summary>
        public T* Ptr;
        
        /// <summary>
        /// Points to the next segment in the linked list of available segments (if any).
        /// </summary>
        /// <remarks>This will be null if the segment is not linked to next segment.</remarks>
        public LinkedSegment<T>* Next;
        
        /// <summary>
        /// The current length of the segment in number of elements currently stored within this segment.
        /// </summary>
        public int Length;
        
        /// <summary>
        /// The maximum capacity of the segment in number of elements.
        /// </summary>
        public int Capacity;

        /// <summary>
        /// Allocates and initializes a new segment of the given capacity for items of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="capacity">The capacity of the segment in number of elements of type
        /// <typeparam name="T"></typeparam> (excluding header allocation size).</param>
        /// <param name="allocator">The allocator to be use to allocate the segment</param>
        /// <returns>Pointer to allocated segment.</returns>
        public static LinkedSegment<T>* Create(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException($"Capacity must be positive, but was {capacity}.");
            
            var segment = UnsafeUtils.AllocateHeaderAndData<LinkedSegment<T>, T>(
                allocator, capacity, out var data);
            segment->Ptr = data;
            segment->Next = null;
            segment->Length = 0;
            segment->Capacity = capacity;
            
            return segment;
        }

        /// <summary>
        /// Destroys the given segment and deallocates associated memory, but does not destroy linked segments.
        /// </summary>
        /// <param name="segment">The segment to be destroyed.</param>
        /// <param name="allocator">The associated allocator (must be same that was used to allocate the segment).</param>
        public static void Destroy(LinkedSegment<T>* segment, AllocatorManager.AllocatorHandle allocator)
        {
#if INPUT_SYSTEM_COALLOCATE
            AllocatorManager.Free(allocator, segment);
#else
            AllocatorManager.Free(allocator, segment->Ptr);
            AllocatorManager.Free(allocator, segment);
#endif // INPUT_SYSTEM_COALLOCATE
        }
        
        /// <summary>
        /// Destroys a range of segments and deallocates associated memory.
        /// </summary>
        /// <param name="first">Pointer to the first segment (inclusive) of the range to be destroyed.</param>
        /// <param name="last">Pointer to the last segment (exclusive) of the range to be destroyed.</param>
        /// <param name="allocator">The associated allocator (must be the same as was used to allocate segments).</param>
        /// <returns><paramref name="last"/> effectively pointing to the new beginning of the linked list of
        /// segments if <paramref name="first"/> was the beginning before the range was destroyed.</returns>
        public static LinkedSegment<T>* DestroyRange(LinkedSegment<T>* first, LinkedSegment<T>* last, 
            AllocatorManager.AllocatorHandle allocator)
        {
            while (first != last)
            {
                var temp = first;
                first = first->Next;
                Destroy(temp, allocator);
            }
            return last;
        }

        // TODO Remove if not needed
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
        private static void CheckAlignment(LinkedSegment<T>* ptr, AllocatorManager.AllocatorHandle allocator)
        {
            if (!CollectionHelper.IsAligned(ptr, UnsafeUtility.AlignOf<LinkedSegment<T>>()) ||
                !CollectionHelper.IsAligned(ptr->Ptr, UnsafeUtility.AlignOf<T>()))
            {
                Destroy(ptr, allocator);
                throw new ArgumentException("Invalid segment memory alignment");
            }
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            if ((IntPtr)Ptr == IntPtr.Zero)
                throw new Exception("Container not created");

            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        public struct Enumerator : IEnumerator<T>
        {
            private readonly LinkedSegment<T> m_ContainerPtr;
            private int m_Index;
            private T m_Value;
            
            public Enumerator(in LinkedSegment<T> segment)
            {
                m_ContainerPtr = segment;
                m_Index = -1;
                m_Value = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (++m_Index < m_ContainerPtr.Length)
                {
                    //AtomicSafetyHandle.CheckReadAndThrow(this.m_Array.m_Safety);
                    m_Value = UnsafeUtility.ReadArrayElement<T>(m_ContainerPtr.Ptr, m_Index);
                    return true;
                }

                m_Value = default(T);
                return false;
            }

            public void Reset()
            {
                m_Index = -1;
            }
            
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Value;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (object) Current;
            }

            public void Dispose()
            {
            }
        }

        public LinkedSegment<T>.Enumerator GetEnumerator() => new LinkedSegment<T>.Enumerator(in this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>) this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();
    }
}