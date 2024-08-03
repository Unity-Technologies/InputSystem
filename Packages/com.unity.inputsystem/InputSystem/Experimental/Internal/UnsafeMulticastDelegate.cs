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

    static class UnsafeEventHandlerHelper
    {
        
    }

    internal struct UnsafeDelegate
    {
        private static AllocatorManager.AllocatorHandle _allocator = AllocatorManager.Persistent;
        
        private struct Item
        {
            public int Length;
            public int RefCount;
        }

        // Disposes a delegate if not null pointer, invoking with null pointer has no side effects.
        public static unsafe void Dispose(ref IntPtr d)
        {
            if (d == IntPtr.Zero)
                return;
            Destroy((Item*)d);
            d = IntPtr.Zero;
        }
        
        // Adds a callback to the given delegate which may be null.
        public static unsafe void Add(ref IntPtr d, IntPtr callback)
        {;
            var previous = d; 
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Combine((Item*)previous, (void*)callback);
                previous = Interlocked.CompareExchange(ref d, ptr, before);
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
        
        // Removes a callback from the given delegate if it exists
        public static unsafe void Remove(ref IntPtr d, IntPtr callback)
        {;
            var previous = d; // Increase ref count of handler
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Remove((Item*)previous, (void*)callback);
                if (ptr == before)
                    return; // Not found
                previous = Interlocked.CompareExchange(ref d, ptr, before);
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
        
        // Invokes a delegate taking a single argument. Note that its a responsibility of the caller that type match.
        public static unsafe void Invoke<TArg1>(IntPtr d, TArg1 arg1)
        {
            var header = (Item*)d;
            var handlers = (delegate*<TArg1, void>*)(header + 1);
            var n = header->Length;
            for (var i = 0; i < n; ++i)
                handlers[i](arg1);        
        }
        
        // Invokes a delegate taking two arguments. Note that its a responsibility of the caller that types match.
        public static unsafe void Invoke<TArg1, TArg2>(IntPtr d, TArg1 arg1, TArg2 arg2)
        {
            var header = (Item*)d;
            var handlers = (delegate*<TArg1, TArg2, void>*)(header + 1);
            var n = header->Length;
            for (var i = 0; i < n; ++i)
                handlers[i](arg1, arg2);        
        }
        
        // Combines a delegate with a callback. Note that existing may be null.
        private static unsafe Item* Combine(Item* existing, [NotNull] void* callback)
        {
            var oldSize = existing == null ? 0 : existing->Length;
            var sizeOf = sizeof(delegate*<void*, void>);
            var item = Allocate(oldSize + 1, out var dst);
            if (oldSize != 0)
                UnsafeUtility.MemCpy(dst, existing + 1, sizeOf * oldSize);
            dst[oldSize] = callback;
            return item;
        }
        
        // Removes a callback from an existing delegate
        private static unsafe Item* Remove([NotNull] Item* existing, [NotNull] void* callback)
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
        
        // Allocates and constructs an item of given length and returns callback array by reference
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Item* Allocate(int length, out void** callbacks)
        {
            var item = (Item*)_allocator.Allocate(ComputeSizeBytes(length), UnsafeUtility.AlignOf<Item>());
            item->Length = length;
            item->RefCount = 1;
            callbacks = (void**)(item + 1);
            return item;
        }
        
        // Unconditionally destroys and deallocates an item.
        private static unsafe void Destroy([NotNull] Item* item)
        {
            AllocatorManager.Free(_allocator, item, ComputeSizeBytes(item->Length), 
                UnsafeUtility.AlignOf<Item>());
        }
        
        // Computes the total size in bytes of an item with given length.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeSizeBytes(int length)
        {
            return sizeof(Item) + length * sizeof(delegate*<void*, void>);
        }
        
        // Increases the reference count.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void AddRef(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero) 
                Interlocked.Increment(ref ((Item*)ptr)->RefCount);
        }
        
        // Decreases the reference count and cleans up resources if count reaches zero.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Release(IntPtr ptr)
        {
            var item = (Item*)ptr;
            if (item != null && Interlocked.Decrement(ref item->RefCount) == 0)
                Destroy(item);
        }
    }
    
    internal struct Callback
    {
        public IntPtr Function;
        public IntPtr Data;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeDelegate<T> 
    {
        internal delegate*<T, void> m_Ptr;
        public UnsafeDelegate(delegate*<T, void> callback)
        {
            m_Ptr = callback;
        }
        public unsafe void Invoke(T value) => m_Ptr(value);
        public IntPtr ToIntPtr() => (IntPtr)m_Ptr;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct UnsafeDelegate<T1, T2> 
    {
        internal unsafe delegate*<in T1, in T2, void> m_Ptr; // TODO Just use IntPtr?
        public unsafe void Invoke(T1 arg1, T2 arg2) => m_Ptr(arg1, arg2);
    }
    
    struct UnsafeEventHandler<T> : IDisposable
    {
        private IntPtr m_Ptr;
        public void Dispose() => UnsafeDelegate.Dispose(ref m_Ptr);
        public unsafe void Add(UnsafeDelegate<T> d) => Add((IntPtr)d.m_Ptr);
        public unsafe void Remove(UnsafeDelegate<T> d) => Remove((IntPtr)d.m_Ptr);

        internal unsafe void Add(IntPtr callback) => UnsafeDelegate.Add(ref m_Ptr, callback);
        internal unsafe void Remove(IntPtr callback) => UnsafeDelegate.Remove(ref m_Ptr, callback);
        
        public unsafe void Invoke(T arg0)
        {
            if (m_Ptr != IntPtr.Zero)
                UnsafeDelegate.Invoke(m_Ptr, arg0);   
        }
    }
    
    struct UnsafeEventHandler<T1, T2> : IDisposable
    {
        private IntPtr m_Ptr;
        public void Dispose() => UnsafeDelegate.Dispose(ref m_Ptr);
        public unsafe void Add(UnsafeDelegate<T1, T2> d) => 
            UnsafeDelegate.Add(ref m_Ptr, (IntPtr)d.m_Ptr);
        public unsafe void Remove(UnsafeDelegate<T1, T2> d) => 
            UnsafeDelegate.Remove(ref m_Ptr, (IntPtr)d.m_Ptr);

        public unsafe void Invoke(T1 arg1, T2 arg2)
        {
            if (m_Ptr != IntPtr.Zero) 
                UnsafeDelegate.Invoke(m_Ptr, arg1, arg2);   
        }
    }
}
