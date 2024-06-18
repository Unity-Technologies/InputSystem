using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityEngine.InputSystem.Experimental
{
    // If we use a segment approach, we will adapt segment size to fit received frequency rate.
    // We could link segments by arranging segments as a lock free linked list.
    // We could allocate segments from a custom allocator holding chunks and free list.
    // For same originating thread we would instead just 
    
    // https://devblogs.microsoft.com/oldnewthing/20210510-00/?p=105200

    /*internal unsafe struct MemoryPool
    {
        private NativeArray<NativeArray<char>> m_Chunks;

        private readonly int chunkSize;

        public MemoryPool(int initialChunks)
            : this(initialChunks, 4096 * 16. AllocatorManager.AllocatorHandle allocator)
        {
        }
        
        public MemoryPool(int initialChunks, int chunkSize = 4096 * 16, AllocatorManager.AllocatorHandle allocator)
        {
            
        }
    }*/

    

    internal static class NativeUtils
    {
        public static int AlignUp(int value, int alignment)
        {
            return value + (-value & (alignment - 1));
        }
    }
    
// // Represents a raw memory block with support for allocation/deallocation as well as read/write
//     // support. Natural alignment is used unless otherwise specified. This raw segment doesn't 
//     // support concurrency.
//     [StructLayout(LayoutKind.Sequential)]
//     internal unsafe struct UnsafeSegment
//     {
//         public void* data;
//         public UnsafeSegment* next;
//         public int capacity;
//         public int length;
//
//         private static readonly int headerAlignment = UnsafeUtility.AlignOf<UnsafeSegment>();
//         private static readonly int headerSizeBytes = UnsafeUtility.SizeOf<UnsafeSegment>();
//         
//         public static void* AllocateSegment(int sizeOf, int alignOf, AllocatorManager.AllocatorHandle allocator, out void* data)
//         {
//             var maxAlign = Mathf.Max(headerAlignment, alignOf);
//             var extendedHeaderSizeBytes = CollectionHelper.Align(headerSizeBytes, alignOf);
//             var totalSizeBytes = alignOf > 16 ? sizeOf + headerSizeBytes + alignOf : headerAlignment;
//             var effectiveAlignment = Mathf.Max(alignOf, headerAlignment);
//             dataOffset = headerSizeBytes;
//             var ptr = allocator.Allocate(sizeOf: totalSizeBytes, alignOf: headerAlignment, 1);
//             ulong ptrOffset = (ulong)ptr;
//             data = ((byte*)ptr) + dataOffset;
//             return ptr;
//         }
//
//         public static void DeallocateSegment(void* ptr, AllocatorManager.AllocatorHandle allocator)
//         {
//             AllocatorManager.Free(allocator, ptr);
//         }
//
//         private static T* Offset<T>(void* ptr, int offset) where T : unmanaged
//         {
//             return (T*)(((byte*)ptr) + offset);
//         }
//         
//         public static UnsafeSegment* Create(int sizeOf, int alignOf, AllocatorManager.AllocatorHandle allocator)
//         {
//             var segment = (UnsafeSegment*)AllocateSegment(sizeOf, alignOf, allocator, out int dataOffset);
//             //segment->data = 
//             
//             // Compute required allocation size taking both segment header and data alignment into account
//             // to make sure both header and data region are properly aligned.
//             //var headerAlignment = UnsafeUtility.AlignOf<UnsafeSegment>();
//             //var headerSizeBytes = UnsafeUtility.SizeOf<UnsafeSegment>();
//             //headerSizeBytes = CollectionHelper.Align(headerSizeBytes, alignOf);
//             //var totalSizeBytes = sizeOf + headerSizeBytes;
//             //var effectiveAlignment = Mathf.Max(alignOf, headerAlignment);
//             //var ptr = allocator.Allocate(sizeOf: totalSizeBytes, alignOf: effectiveAlignment, 1);
//             //var segment = (UnsafeSegment*)ptr;
//             if (!CollectionHelper.IsAligned(segment, UnsafeUtility.AlignOf<UnsafeSegment>()))
//             {
//                 DeallocateSegment(segment, allocator);
//                 throw new Exception("Invalid alignment");
//             }
//             //UnsafeUtility.MemClear(segment, sizeof(Segment)); // TODO Make optional
//             segment->data = ((byte*)segment) + headerSizeBytes;
//             if (!CollectionHelper.IsAligned(segment->data, alignOf))
//             {
//                 AllocatorManager.Free(allocator, segment);
//                 throw new Exception("Invalid alignment");
//             }
//             segment->capacity = sizeOf;
//             segment->length = 0;
//             return segment;
//         }
//
//         public static void Destroy(UnsafeSegment* segment, AllocatorManager.AllocatorHandle allocator)
//         {
//             AllocatorManager.Free(allocator, segment);
//             segment->next = null;
//             segment->data = null;
//         }
//
//         public static void Write(UnsafeSegment* segment, void* value, long size)
//         {
//             UnsafeUtility.MemCpy(((byte*)segment->data) + segment->length, value, size);
//         }
//     }
//
//     internal unsafe struct UnsafeSegment<T>
//     {
//         public T* data;
//         public UnsafeSegment<T>* next;
//         public int capacity;
//         public int length;
//         
//         public static UnsafeSegment<T>* Create(int capacity, AllocatorManager.AllocatorHandle allocator)
//         {
//             var rawSegment = UnsafeSegment.Create(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
//             
//             // Compute required allocation size taking both segment header and data alignment into account
//             // to make sure both header and data region are properly aligned.
//             var headerAlignment = UnsafeUtility.AlignOf<UnsafeSegment>();
//             var headerSizeBytes = UnsafeUtility.SizeOf<UnsafeSegment>();
//             headerSizeBytes = CollectionHelper.Align(headerSizeBytes, alignOf);
//             var totalSizeBytes = sizeOf + headerSizeBytes;
//             var effectiveAlignment = Mathf.Max(alignOf, headerAlignment);
//             var ptr = allocator.Allocate(sizeOf: totalSizeBytes, alignOf: effectiveAlignment, 1);
//             var segment = (UnsafeSegment*)ptr;
//             if (!CollectionHelper.IsAligned(segment, headerAlignment))
//             {
//                 AllocatorManager.Free(allocator, ptr);
//                 throw new Exception("Invalid alignment");
//             }
//             //UnsafeUtility.MemClear(segment, sizeof(Segment)); // TODO Make optional
//             segment->data = ((byte*)ptr) + headerSizeBytes;
//             if (!CollectionHelper.IsAligned(segment->data, alignOf))
//             {
//                 AllocatorManager.Free(allocator, ptr);
//                 throw new Exception("Invalid alignment");
//             }
//             segment->capacity = sizeOf;
//             segment->length = 0;
//             return segment;
//         }
//     }
    
    // internal unsafe struct UnsafeStream 
    //     : INativeDisposable
    // {
    //     
    //     
    //     [NativeDisableUnsafePtrRestriction] 
    //     private UnsafeSegment* m_Ptr;
    //
    //     private AllocatorManager.AllocatorHandle Allocator;
    //
    //     /*public UnsafeStream(uint elementSizeBytes, uint elementAlignment)
    //     {
    //         
    //     }*/
    //     
    //     public readonly bool IsCreated
    //     {
    //         [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //         get => m_Ptr != null;
    //     }
    //
    //     public void Dispose()
    //     {
    //         //if (!isCreated)
    //         //    return;
    //     }
    //
    //     public JobHandle Dispose(JobHandle inputDeps)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }
    
    internal struct NativeLocalStream : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct NativeStreamSegment
        {
            public void* data;
            public uint length;
            public uint writeIndex;
            public uint readIndex;
            
            public int refCount;
            public NativeStreamSegment* next;
        }
        
        private unsafe NativeStreamSegment* m_Segment;
        private readonly Allocator m_Allocator;

        public unsafe NativeLocalStream(int capacity, int alignment, int elementSize, Allocator allocator)
        {
            m_Segment = AllocateSegment(capacity, alignment, elementSize, allocator);
            m_Allocator = allocator;
        }

        public unsafe void Release()
        {
            if (--m_Segment->refCount == 0)
                Destroy();
        }
        
        private unsafe void Destroy()
        {
            if (m_Segment == null)
                return;
            UnsafeUtility.Free(m_Segment, m_Allocator);
            m_Segment = null;
        }

        private static int AlignUp(int value, int alignment)
        {
            return value + (-value & (alignment - 1));
        }

        private static unsafe NativeStreamSegment* AllocateSegment(int capacity, int elementAlignment, int elementSize, Allocator allocator)
        {
            // Compute required allocation size taking both segment header and data alignment into account
            var headerAlignment = UnsafeUtility.AlignOf<NativeStreamSegment>();
            var headerSizeBytes = UnsafeUtility.SizeOf<NativeStreamSegment>();
            headerSizeBytes = AlignUp(headerSizeBytes, elementAlignment); 
            var totalSizeBytes = (capacity + 1) * elementSize + headerSizeBytes;
            
            // Allocate the required memory for segment + data
            var segment = (NativeStreamSegment*)UnsafeUtility.Malloc(totalSizeBytes, headerAlignment, allocator);
            UnsafeUtility.MemClear(segment, sizeof(NativeStreamSegment));
            segment->refCount = 1;
            return segment;
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}