using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tests.InputSystem.Experimental
{
    unsafe struct TempPointer<T> : IDisposable where T : unmanaged
    {
        public static TempPointer<T> Create()
        {
            var ptr = UnsafeUtility.Malloc(sizeof(T), UnsafeUtility.AlignOf<T>(), Allocator.Temp);
            return new TempPointer<T>() { pointer = (T*)ptr };
        }

        public ref T value => ref *pointer;

        public unsafe T* pointer { get; private set; }

        public void Dispose()
        {
            if (pointer != null)
            {
                UnsafeUtility.Free(pointer, Allocator.Temp);
                pointer = null;
            }
        }
    }
}