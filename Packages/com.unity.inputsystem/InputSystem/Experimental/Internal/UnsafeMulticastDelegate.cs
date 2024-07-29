using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    // https://stackoverflow.com/questions/3522361/add-delegate-to-event-thread-safety
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code
    internal struct UnsafeMulticastDelegate : IDisposable
    {
        private AllocatorManager.AllocatorHandle m_Allocator;
        private IntPtr m_Delegates;

        private unsafe struct Item
        {
            public int Length;
            public int RefCount;
            public delegate*<void*, void> Callbacks;
        }

        public UnsafeMulticastDelegate(AllocatorManager.AllocatorHandle allocator)
        {
            m_Delegates = IntPtr.Zero;
            m_Allocator = allocator;
        }

        public unsafe void Dispose()
        {
            if (m_Delegates == IntPtr.Zero) 
                return;
            
            AllocatorManager.Free(m_Allocator, m_Delegates.ToPointer());
            m_Delegates = IntPtr.Zero;
        }
        
        private static unsafe void AddRef(IntPtr ptr)
        {
            //ushort* p = (ushort*)ptr;
            //Interlocked.Increment()
            //p[1];
        }
        
        private unsafe void Release(IntPtr ptr)
        {
            AllocatorManager.Free(m_Allocator, (void*)ptr);
            //ushort* p = (ushort*)d;
            //--p[1]; // TODO If interlocked it need to be a valid adress
        }

        public unsafe int length
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
        }

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

        private unsafe delegate*<void*, void>* Remove(delegate*<void*, void>* existing, 
            delegate*<void*, void> callback)
        {
            var oldSize = Decode(out var src);
            var index = oldSize;
            while (--index >= 0 && src[index] != callback) { }
            if (index < 0)
                return existing; // Not found
            if (oldSize == 1)
                return null;     // Eliminating item results in empty array
            
            var sizeOf = sizeof(delegate*<void*, void>);
            var ptr = (delegate*<void*, void>*)m_Allocator.Allocate(sizeOf, 8, oldSize);
            var dst = ptr + 1;
            var offset = index * sizeOf;
            var newSize = oldSize - 1;
            UnsafeUtility.MemCpy(dst, src, offset);
            UnsafeUtility.MemCpy(dst + offset, src + index, newSize - index);
            return Encode(ptr, newSize);
        }
        
        private unsafe delegate*<void*, void>* Combine(delegate*<void*, void>* existing, 
            delegate*<void*, void> callback)
        {
            var oldSize = existing == null ? 0 : *(ushort*)existing!;
            var newSize = oldSize + 1;
            var sizeOf = sizeof(delegate*<void*, void>);
            var ptr = (delegate*<void*, void>*)m_Allocator.Allocate(sizeOf, 8, newSize + 1);
            if (oldSize != 0)
                UnsafeUtility.MemCpy(ptr + 1, existing + 1, sizeOf * oldSize);
            ptr[newSize] = callback;
            return Encode(ptr, newSize);
        }
        
        public unsafe void Add(delegate*<void*, void> callback)
        {;
            var previous = m_Delegates; 
            for(;;)
            {
                var before = previous;
                AddRef(before);
                var ptr = (IntPtr)Combine((delegate*<void*, void>*)previous, callback);
                previous = Interlocked.CompareExchange(ref m_Delegates, ptr, before);
                if (previous == before)
                {
                    Release(before);
                    
                    // If we reach this point previous is no longer stored in m_Delegates, but other threads
                    // may still reference it.
                    // TODO Release handler
                    //AllocatorManager.Free(m_Allocator, (void*)previous);
                    break;
                }
                
                // We can safely destroy ptr directly if CAS failed since this is the only thread that have seen it.
                AllocatorManager.Free(m_Allocator, (void*)ptr);
                    
                 // TODO Only deallocate if size increased, otherwise we may reuse existing buffer
            }
        }

        /// <summary>
        /// Removes a callback from this multi-cast delegate. 
        /// </summary>
        /// <remarks>This function may only be used when the delegate is only accessed on a single thread.</remarks>
        /// <param name="callback">The callback to be removed</param>
        public unsafe void RemoveFast(delegate*<void*, void> callback)
        {
            var previous = m_Delegates;
            m_Delegates = (IntPtr)Remove((delegate*<void*, void>*)previous, callback);
            if (m_Delegates == previous)
                return; // callback not found
            AllocatorManager.Free(m_Allocator, (void*)previous);
        }
        
        public unsafe void Remove(delegate*<void*, void> callback)
        {;
            var previous = m_Delegates; // Increase ref count of handler
            for(;;)
            {
                var before = previous;
                var ptr = (IntPtr)Remove((delegate*<void*, void>*)previous, callback);
                if (ptr == before)
                    return; // Not found
                previous = Interlocked.CompareExchange(ref m_Delegates, ptr, before);
                if (previous == before)
                {
                    AllocatorManager.Free(m_Allocator, (void*)previous);
                    break;
                }
                
                // Decrease ref count of handler2
                AllocatorManager.Free(m_Allocator, (void*)ptr);
                    
                // TODO Only deallocate if size increased, otherwise we may reuse existing buffer
            }
        }

        public unsafe void Invoke(void* arg)
        {
            // TODO Would need to increase ref count here
            var n = Decode(out delegate*<void*, void>* handlers);
            for (var i = 0; i < n; ++i)
                handlers[i](arg);
            // TODO Would need to decrease ref count here
        }
    }
}
