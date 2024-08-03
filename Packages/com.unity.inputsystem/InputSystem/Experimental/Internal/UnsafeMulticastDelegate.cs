using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.Experimental
{
    // https://stackoverflow.com/questions/3522361/add-delegate-to-event-thread-safety
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnsafeMulticastDelegate : IUnsafeMulticastDelegate, IDisposable
    {
        private static AllocatorManager.AllocatorHandle _allocator = AllocatorManager.Persistent;
        
        private IntPtr m_Delegates;

        public unsafe void Dispose()
        {
            if (m_Delegates == IntPtr.Zero) 
                return; // Not initialized
            
            Destroy((Item*)m_Delegates);
            m_Delegates = IntPtr.Zero;
        }
        
        // Header defining subsequent list of callbacks
        private struct Item
        {
            public int Length;
            public int RefCount;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void AddRef(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero) 
                Interlocked.Increment(ref ((Item*)ptr)->RefCount);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Release(IntPtr ptr)
        {
            var item = (Item*)ptr;
            if (item != null && Interlocked.Decrement(ref item->RefCount) == 0)
                Destroy(item);
        }

        // Removes a callback frmo an existing delegate
        private unsafe Item* Remove([NotNull] Item* existing, [NotNull] delegate*<void*, void> callback)
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
                return null; // Eliminating item results in empty array
            
            var newSize = oldSize - 1;
            var item = Allocate(newSize, out var dst);
            var offset = index * sizeof(delegate*<void*, void>);
            UnsafeUtility.MemCpy(dst, src, offset);
            UnsafeUtility.MemCpy(dst + offset, src + index, newSize - index);
            return item;
        }

        // Combines an existing delegate with the given callback
        private unsafe Item* Combine([NotNull] Item* existing, [NotNull] delegate*<void*, void> callback)
        {
            var oldSize = existing == null ? 0 : existing->Length;
            var sizeOf = sizeof(delegate*<void*, void>);
            var item = Allocate(oldSize + 1, out var dst);
            if (oldSize != 0)
                UnsafeUtility.MemCpy(dst, existing + 1, sizeOf * oldSize);
            dst[oldSize] = callback;
            return item;
        }
        
        // Computes the total size in bytes of an item with given length
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeSizeBytes(int length)
        {
            return sizeof(Item) + length * sizeof(delegate*<void*, void>);
        }

        // Allocates and constructs an item of given length and returns callback array by reference
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Item* Allocate(int length, out delegate*<void*, void>* callbacks)
        {
            var item = (Item*)_allocator.Allocate(ComputeSizeBytes(length), UnsafeUtility.AlignOf<Item>());
            item->Length = length;
            item->RefCount = 1;
            callbacks = (delegate*<void*, void>*)(item + 1);
            return item;
        }
        
        // Destroys and deallocates an item
        private unsafe void Destroy([NotNull] Item* item)
        {
            AllocatorManager.Free(_allocator, item, ComputeSizeBytes(item->Length), 
                UnsafeUtility.AlignOf<Item>());
        }
        
        #region IUnsafeDelegate
        
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
                AllocatorManager.Free(_allocator, (void*)ptr);
                    
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
                AllocatorManager.Free(_allocator, (void*)ptr);
                    
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
        
        #endregion
    }
}
