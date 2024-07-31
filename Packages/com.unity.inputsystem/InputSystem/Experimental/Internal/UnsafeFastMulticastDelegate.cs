using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    internal struct UnsafeFastMulticastDelegate : IDisposable
    {
        private static AllocatorManager.AllocatorHandle _allocator = AllocatorManager.Persistent;
        private IntPtr m_Ptr;

        private const int SizeOf = 8;
        private const int AlignOf = 8;
        
        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct Item
        {
            [FieldOffset(0)] public int length;
            [FieldOffset(8)] public delegate*<void*, void> callback;
        }

        private static unsafe int ComputeSizeBytes(int n)
        {
            return (n - 1) * sizeof(delegate*<void*, void>) + sizeof(Item);
        }
        
        public void Dispose()
        {
            if (m_Ptr == IntPtr.Zero) 
                return; // Not initialized
            Destroy(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }
        
        public unsafe void Add(delegate*<void*, void> callback)
        {;
            var previous = m_Ptr;
            m_Ptr = Combine(m_Ptr, callback);
            if (previous != IntPtr.Zero)
                Destroy(previous);
        }

        public unsafe void Remove(delegate*<void*, void> callback)
        {
            var previous = m_Ptr;
            m_Ptr = Remove(m_Ptr, callback);
            if (previous != IntPtr.Zero)
                Destroy(previous);
        }
        
        private unsafe void Destroy(IntPtr ptr)
        {
            var item = (Item*)ptr;
            AllocatorManager.Free(_allocator, item, ComputeSizeBytes(item->length), AlignOf);
        }
        
        private unsafe IntPtr Remove(IntPtr ptr, delegate*<void*, void> callback)
        {
            var existing = (Item*)ptr;
            if (existing == null)
                return IntPtr.Zero; // Empty
            var oldSize = existing->length; //Decode(out var src);
            var index = oldSize;
            var src = &existing->callback;
            while (--index >= 0 && src[index] != callback) { }
            if (index < 0)
                return ptr; // Not found
            if (oldSize == 1)
                return IntPtr.Zero;     // Eliminating item results in empty array
            
            var newSize = oldSize - 1;
            var item = (Item*)_allocator.Allocate(ComputeSizeBytes(newSize), 8);
            item->length = newSize;
            var dst = &item->callback;
            var offset = index * SizeOf;
            UnsafeUtility.MemCpy(dst, src, offset);
            UnsafeUtility.MemCpy(dst + offset, src + index, newSize - index);
            return (IntPtr)item;
        }
        
        private unsafe IntPtr Combine(IntPtr ptr, delegate*<void*, void> callback)
        {
            var existing = (Item*)ptr;
            var oldSize = existing == null ? 0 : existing->length;
            var newSize = oldSize + 1;
            var sizeOf = sizeof(delegate*<void*, void>);
            var allocator = AllocatorManager.Persistent;
            var item = (Item*)allocator.Allocate(sizeof(Item) + oldSize * sizeOf, 8);
            item->length = newSize;
            var dst = &item->callback;
            if (oldSize != 0)
                UnsafeUtility.MemCpy(dst, &existing->callback, sizeOf * oldSize);
            dst[oldSize] = callback;
            return (IntPtr)item;
        }
    }
}