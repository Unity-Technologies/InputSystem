using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    internal unsafe static class UnsafeUtils
    {
        internal static long Distance(void* first, void* last)
        {
            var diff = ((ulong)last) - ((ulong)first);
            return (long)diff;
        }
        
        internal static void* Offset(void* ptr, int offset)
        {
            return (void*)(((ulong)ptr) + (ulong)offset);
        }
        
        internal static void* OffsetAndAlignUp(void* ptr, int offset, int alignment)
        {
            return (void*)CollectionHelper.Align((ulong)((IntPtr)ptr + offset), (ulong)alignment);
        }
        
        internal static void* AlignUp(void* ptr, int alignment)
        {
            return (void*)CollectionHelper.Align(((ulong)ptr), (ulong)alignment);
        }

        internal struct HeaderAndData 
        {
            public HeaderAndData(void* header, void* data)
            {
                this.Header = header;
                this.Data = data;
            }
            
            public void* Header;
            public void* Data;
        }
        
        internal struct HeaderAndData<THeader, TData> 
            where THeader : unmanaged
            where TData : unmanaged
        {
            public THeader* header;
            public TData* data;
        }
        
        /// <summary>
        /// Co-allocates THeader and TData in the same memory region. 
        /// </summary>
        /// <param name="allocator">The allocator to be used for allocating or freeing memory.</param>
        /// <param name="itemCount">The number of data element to be allocated.</param>
        /// <typeparam name="THeader">The header type.</typeparam>
        /// <typeparam name="TData">The data type.</typeparam>
        /// <returns>HeaderAndData object providing resulting pointers.</returns>
        internal static THeader* AllocateHeaderAndData<THeader, TData>(
            AllocatorManager.AllocatorHandle allocator, int itemCount, out TData* data) 
            where THeader : unmanaged 
            where TData : unmanaged
        {
            // Co-allocate segment header and data in the same memory region.
            // Note that we pad segment size based on alignment requirement of type T for a given alignment guarantee
            // of the underlying allocator.
            var headerSizeBytes = UnsafeUtility.SizeOf<THeader>();
            var itemAlignment = UnsafeUtility.AlignOf<TData>();
            var totalSizeBytes = headerSizeBytes + (itemCount * UnsafeUtility.SizeOf<TData>()) + itemAlignment;
            var ptr = (THeader*)allocator.Allocate(sizeOf: totalSizeBytes, alignOf: UnsafeUtility.AlignOf<THeader>());
            
            // Initialize segment with pointer offset into allocated memory block to match alignment.
            data = (TData*)AlignUp(Offset(ptr, headerSizeBytes), itemAlignment); 
            
            CheckAlignment(ptr, data, allocator);

            return ptr;
        }
        
        internal static HeaderAndData AllocateHeaderAndData(int headerSizeOf, int headerAlignment,
            int itemSizeOf, int itemAlignment, int itemCount, 
            AllocatorManager.AllocatorHandle allocator) 
        {
            // Co-allocate segment header and data in the same memory region.
            // Note that we pad segment size based on alignment requirement of type T for a given alignment guarantee
            // of the underlying allocator.
            var totalSizeBytes = (itemCount * itemSizeOf) + itemAlignment + headerSizeOf;
            var ptr = allocator.Allocate(totalSizeBytes, headerAlignment);
            
            // Initialize segment with pointer offset into allocated memory block to match alignment.
            var data = UnsafeUtils.AlignUp(UnsafeUtils.Offset(ptr, headerSizeOf), itemAlignment); 
            
            CheckAlignment(ptr, headerAlignment, data, itemAlignment, allocator);

            return new HeaderAndData(ptr, data);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckAlignment(void* header, int headerAlignment, void* data, int dataAlignment, 
            AllocatorManager.AllocatorHandle allocator) 
        {
            if (!CollectionHelper.IsAligned(header, headerAlignment))
            {
                AllocatorManager.Free(allocator, header);
                throw new ArgumentException("Invalid segment memory alignment");
            }
            
            if (!CollectionHelper.IsAligned(data, dataAlignment))
            {
                AllocatorManager.Free(allocator, header);
                throw new ArgumentException("Invalid segment data memory alignment");
            }
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckAlignment<THeader, TData>(THeader* header, TData* data, 
            AllocatorManager.AllocatorHandle allocator) 
            where THeader : unmanaged 
            where TData : unmanaged
        {
            if (!CollectionHelper.IsAligned(header, UnsafeUtility.AlignOf<THeader>()) ||
                !CollectionHelper.IsAligned(data, UnsafeUtility.AlignOf<TData>()))
            {
                AllocatorManager.Free(allocator, header);
                throw new ArgumentException("Invalid segment memory alignment");
            }
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
    }
}