using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UI;

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

    // https://stackoverflow.com/questions/3522361/add-delegate-to-event-thread-safety
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnsafeDelegate : IUnsafeMulticastDelegate, IDisposable
    {
        private AllocatorManager.AllocatorHandle m_Allocator;
        private IntPtr m_Delegates;

        private struct Item
        {
            public int Length;
            public int RefCount;
        }

        public UnsafeDelegate(AllocatorManager.AllocatorHandle allocator)
        {
            m_Delegates = IntPtr.Zero;
            m_Allocator = allocator;
        }

        public unsafe void Dispose()
        {
            if (m_Delegates == IntPtr.Zero) 
                return; // Not initialized
            Destroy(m_Delegates);
            m_Delegates = IntPtr.Zero;
        }
        
        private static unsafe void AddRef(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) 
                return;
            var header = (Item*)ptr;
            Interlocked.Increment(ref header->RefCount);
        }
        
        private unsafe void Release(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) 
                return;
            var item = (Item*)ptr;
            if (Interlocked.Decrement(ref item->RefCount) == 0)
                Destroy(ptr);
        }

        private unsafe void Destroy(IntPtr ptr)
        {
            var item = (Item*)ptr;
            var sizeBytes = sizeof(Item) + item->Length * sizeof(delegate*<void*, void>);
            AllocatorManager.Free(m_Allocator, item, sizeBytes, 8);
        }

        /*public unsafe int length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Delegates == IntPtr.Zero ? 0 : *(ushort*)m_Delegates;
        }

        private unsafe delegate*<void*, void>* Encode(delegate*<void*, void>* d, int length)
        {
            var p = (ushort*)d;
            p[0] = (ushort)length;
            p[1] = 1;
            return d;
        }*/

        private unsafe int Decode(out delegate*<void*, void>* handlers)
        {
            if (m_Delegates == IntPtr.Zero)
            {
                handlers = null;
                return 0;
            }
            var p = (ushort*)m_Delegates;
            handlers = (delegate*<void*, void>*)m_Delegates + 1;
            return p![0];
        }

        private unsafe Item* Remove(Item* existing, delegate*<void*, void> callback)
        {
            if (existing == null)
                return null; // Empty
            var oldSize = existing->Length; //Decode(out var src);
            var index = oldSize;
            var src = (delegate*<void*, void>*)(existing + 1);
            while (--index >= 0 && src[index] != callback) { } // TODO See https://stackoverflow.com/questions/66630082/what-are-alternatives-to-comparing-unmanaged-function-pointers-in-c-sharp-how-t#:~:text=error%20CS8909%3A%20Comparison%20of%20function,always%20yield%20the%20same%20pointer.
            if (index < 0)
                return existing; // Not found
            if (oldSize == 1)
                return null;     // Eliminating item results in empty array
            
            var sizeOf = sizeof(delegate*<void*, void>);
            var newSize = oldSize - 1;
            var header = (Item*)m_Allocator.Allocate(sizeof(Item) + newSize * sizeOf, 8);
            header->Length = newSize;
            header->RefCount = 1;
            var dst = (delegate*<void*, void>*)(header + 1);
            var offset = index * sizeOf;
            UnsafeUtility.MemCpy(dst, src, offset);
            UnsafeUtility.MemCpy(dst + offset, src + index, newSize - index);
            return header;
        }
        
        private unsafe Item* Combine(Item* existing, delegate*<void*, void> callback)
        {
            var oldSize = existing == null ? 0 : existing->Length;
            var newSize = oldSize + 1;
            var sizeOf = sizeof(delegate*<void*, void>);
            var header = (Item*)m_Allocator.Allocate(sizeof(int) * 2 + newSize * sizeOf, 8);
            header->Length = newSize;
            header->RefCount = 1;
            var dst = (delegate*<void*, void>*)(header + 1);
            if (oldSize != 0)
                UnsafeUtility.MemCpy(dst, existing + 1, sizeOf * oldSize);
            dst[oldSize] = callback;
            return header;
        }
        
        public unsafe void Add(delegate*<void*, void> callback)
        {;
            var previous = m_Delegates; 
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Combine((Item*)previous, callback);
                previous = Interlocked.CompareExchange(ref m_Delegates, ptr, before);
                if (previous == before)
                {
                    Release(before);
                    break;
                }
                
                Release(before);
                AllocatorManager.Free(m_Allocator, (void*)ptr);
                    
                 // TODO Only deallocate if size increased, otherwise we may reuse existing buffer
            }
        }
        
        public unsafe void Remove(delegate*<void*, void> callback)
        {;
            var previous = m_Delegates; // Increase ref count of handler
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Remove((Item*)previous, callback);
                if (ptr == before)
                    return; // Not found
                previous = Interlocked.CompareExchange(ref m_Delegates, ptr, before);
                if (previous == before)
                {
                    Release(before);
                    break;
                }
                
                Release(before);
                AllocatorManager.Free(m_Allocator, (void*)ptr);
                    
                // TODO Only deallocate if size increased, otherwise we may reuse existing buffer
            }
        }

        public unsafe void Invoke(void* arg)
        {
            // TODO Would need to increase ref count here
            //var n = Decode(out delegate*<void*, void>* handlers);
            if (m_Delegates == IntPtr.Zero)
                return;
            
            var header = (Item*)m_Delegates;
            var n = header->Length;
            var handlers = (delegate*<void*, void>*)(header + 1);
            for (var i = 0; i < n; ++i)
                handlers[i](arg);
            // TODO Would need to decrease ref count here
        }
    }
}
