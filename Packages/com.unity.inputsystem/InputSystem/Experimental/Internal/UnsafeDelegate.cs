using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    internal struct UnsafeDelegate
    {
        private static AllocatorManager.AllocatorHandle _allocator = AllocatorManager.Persistent;
        
        private struct Item
        {
            public int Length;
            public int RefCount;
        }

        internal unsafe struct Callback : IEquatable<Callback>
        {
            private void* Function;
            private void* Data;

            public Callback(void* function, void* data)
            {
                Function = function;
                Data = data;
            }

            public bool Equals(Callback other)
            {
                return Function == other.Function && Data == other.Data;
            }

            public override bool Equals(object obj)
            {
                return obj is Callback other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(unchecked((int)(long)Function), unchecked((int)(long)Data));
            }

            public static bool operator ==(Callback left, Callback right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Callback left, Callback right)
            {
                return !left.Equals(right);
            }
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
        public static unsafe void Add(ref IntPtr d, Callback callback)
        {;
            var previous = d; 
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Combine((Item*)previous, callback);
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
        public static unsafe void Remove(ref IntPtr d, Callback callback)
        {;
            var previous = d; // Increase ref count of handler
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Remove((Item*)previous, callback);
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
            var handlers = (delegate*<TArg1, void*, void>*)(header + 1);
            var n = header->Length;
            for (var i = 0; i < n; i += 2)
                handlers[i](arg1, handlers[i+1]);    
        }
        
        // Invokes a delegate taking two arguments. Note that its a responsibility of the caller that types match.
        public static unsafe void Invoke<TArg1, TArg2>(IntPtr d, TArg1 arg1, TArg2 arg2)
        {
            var header = (Item*)d;
            var handlers = (delegate*<TArg1, TArg2, void*, void>*)(header + 1);
            var n = header->Length;
            for (var i = 0; i < n; i += 2)
                handlers[i](arg1, arg2, handlers[i+1]);   
        }
        
        // Combines a delegate with a callback. Note that existing may be null.
        private static unsafe Item* Combine(Item* existing, Callback callback)
        {
            var oldSize = existing == null ? 0 : existing->Length;
            var sizeOf = sizeof(Callback);
            var item = Allocate(oldSize + 1, out var dst);
            if (oldSize != 0)
                UnsafeUtility.MemCpy(dst, existing + 1, sizeOf * oldSize);
            dst[oldSize] = callback;
            return item;
        }
        
        // Removes a callback from an existing delegate
        private static unsafe Item* Remove([NotNull] Item* existing, Callback callback)
        {
            if (existing == null)
                return null; // Empty
            var oldSize = existing->Length; //Decode(out var src);
            var index = oldSize;
            var src = (Callback*)(existing + 1);
            while (--index >= 0 && src[index] != callback) { } // TODO See https://stackoverflow.com/questions/66630082/what-are-alternatives-to-comparing-unmanaged-function-pointers-in-c-sharp-how-t#:~:text=error%20CS8909%3A%20Comparison%20of%20function,always%20yield%20the%20same%20pointer.
            if (index < 0)
                return existing; // Not found
            if (oldSize == 1)
                return null; // Eliminating item results in empty array
            
            var newSize = oldSize - 1;
            var item = Allocate(newSize, out var dst);
            var offset = index * sizeof(Callback);
            UnsafeUtility.MemCpy(dst, src, offset);
            UnsafeUtility.MemCpy(dst + offset, src + index, newSize - index);
            return item;
        }
        
        // Allocates and constructs an item of given length and returns callback array by reference
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Item* Allocate(int length, out Callback* callbacks)
        {
            var item = (Item*)_allocator.Allocate(ComputeSizeBytes(length), UnsafeUtility.AlignOf<Item>());
            item->Length = length;
            item->RefCount = 1;
            callbacks = (Callback*)(item + 1);
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
            return sizeof(Item) + length * sizeof(Callback);
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
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeDelegate<T>
    {
        internal delegate*<T, void> m_Ptr;
        private void* m_Data;
        
        public UnsafeDelegate(delegate*<T, void> callback, void* data = null)
        {
            m_Ptr = callback;
            m_Data = data;
        }
        public unsafe void Invoke(T value) => m_Ptr(value);
        public IntPtr ToIntPtr() => (IntPtr)m_Ptr;
        internal UnsafeDelegate.Callback ToCallback() => new (m_Ptr, null);
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeDelegate<T1, T2> 
    {
        internal unsafe delegate*<in T1, in T2, void> m_Ptr; // TODO Just use IntPtr?
        public unsafe void Invoke(T1 arg1, T2 arg2) => m_Ptr(arg1, arg2);
        internal UnsafeDelegate.Callback ToCallback() => new(m_Ptr, null);
    }
    
    struct UnsafeEventHandler<T> : IDisposable // TODO Shouldn't really be called unsafe if not unsafe, maybe StaticDelegate
    {
        private IntPtr m_Ptr;
        public void Dispose() => UnsafeDelegate.Dispose(ref m_Ptr);
        public void Add(UnsafeDelegate<T> d) => Add(d.ToCallback());
        public void Remove(UnsafeDelegate<T> d) => Remove(d.ToCallback());

        internal void Add(UnsafeDelegate.Callback callback) => UnsafeDelegate.Add(ref m_Ptr, callback);
        internal void Remove(UnsafeDelegate.Callback callback) => UnsafeDelegate.Remove(ref m_Ptr, callback);
        
        public void Invoke(T arg0)
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
            UnsafeDelegate.Add(ref m_Ptr, d.ToCallback());
        public unsafe void Remove(UnsafeDelegate<T1, T2> d) => 
            UnsafeDelegate.Remove(ref m_Ptr, d.ToCallback());

        public unsafe void Invoke(T1 arg1, T2 arg2)
        {
            if (m_Ptr != IntPtr.Zero) 
                UnsafeDelegate.Invoke(m_Ptr, arg1, arg2);   
        }
    }
}