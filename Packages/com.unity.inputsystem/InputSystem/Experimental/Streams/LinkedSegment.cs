// TODO Why cannot we coallocate memory, consult Kernel team
#define INPUT_SYSTEM_COALLOCATE

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents a fixed-size segment of consecutive memory to hold values of <typeparamref name="T"/> that is
    /// optionally part of an intrusive linked list of segments.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    ///If INPUT_SYSTEM_COALLOCATE is defined, the segment will coallocate header and data region into a single
    /// allocation. If INPUT_SYSTEM_COALLOCATE is not defined, the segment will perform separate allocations for
    /// header and data.
    /// </remarks>
    internal unsafe struct LinkedSegment<T> where T : unmanaged
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
            
#if INPUT_SYSTEM_COALLOCATE
            // Co-allocate segment header and data in the same memory region.
            // Note that we pad segment size based on alignment requirement of type T.
            var headerSizeBytes = UnsafeUtility.SizeOf<LinkedSegment<T>>();
            var itemsSizeBytes = capacity * UnsafeUtility.SizeOf<T>();
            var itemAlignment = UnsafeUtility.AlignOf<T>();
            var totalSizeBytes = headerSizeBytes + itemsSizeBytes + itemAlignment;
            var ptr = allocator.Allocate(sizeOf: totalSizeBytes, alignOf: UnsafeUtility.AlignOf<LinkedSegment<T>>());
            
            // Initialize segment with pointer offset into allocated memory block to match alignment.
            var segment = (LinkedSegment<T>*)ptr;
            segment->Ptr = (T*)AlignUp(Offset(ptr, headerSizeBytes), UnsafeUtility.AlignOf<T>()); //(T*)((ulong)ptr) + headerSizeBytes;
#else
            // Perform separate allocations for segment header and data
            var segment = (LinkedSegment<T>*)allocator.Allocate(sizeof(LinkedSegment<T>), UnsafeUtility.AlignOf<LinkedSegment<T>>());
            segment->Ptr = (T*)allocator.Allocate(sizeof(T), UnsafeUtility.AlignOf<T>(), capacity);
#endif // INPUT_SYSTEM_COALLOCATE
            segment->Next = null;
            segment->Length = 0;
            segment->Capacity = capacity;
            
            CheckAlignment(segment, allocator);
            
            return segment;
        }

        private static void* Offset(void* ptr, int offset)
        {
            return (void*)(((ulong)ptr) + (ulong)offset);
        }
        
        private static void* AlignUp(void* ptr, int alignment)
        {
            return (void*)CollectionHelper.Align(((ulong)ptr), (ulong)alignment);
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
    }
}