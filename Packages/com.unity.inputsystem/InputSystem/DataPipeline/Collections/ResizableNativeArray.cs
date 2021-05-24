using System;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline.Collections
{
    // TODO our native callback is missing temp alloc scope, so I'm using persistent allocations for _now_.
    // TODO use collections proper
    public unsafe struct ResizableNativeArray<T> : IDisposable where T : struct
    {
        // because jobs copy their fields by value, we need a pointer to be able to read modified data back
        private struct Data
        {
            [NativeDisableUnsafePtrRestriction] [NoAlias]
            public void* ptr;

            public int allocationLength;

            public int length;
        }

        public int Length => m_Data[0].length;

        private NativeArray<Data> m_Data;

        private int minAllocationLength;

        private const Allocator k_Label = Allocator.Persistent;

        public ResizableNativeArray(int minLength)
        {
            minAllocationLength = minLength;
            Debug.Assert(minAllocationLength > 0);

            m_Data = new NativeArray<Data>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_Data[0] = new Data
            {
                ptr = UnsafeUtility.Malloc(minAllocationLength * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(),
                    k_Label),
                length = 0,
                allocationLength = minAllocationLength
            };
        }

        public void Clear()
        {
            var data = m_Data[0];
            data.length = 0;
            m_Data[0] = data;
        }

        public void ResizeToFit(int newLength, bool growOnly = false)
        {
            var data = m_Data[0];
            var oldLength = data.length;
            data.length = newLength;

            var newAllocationLength = NextPowerOfTwo(newLength);
            newAllocationLength = newAllocationLength < minAllocationLength ? minAllocationLength : newAllocationLength;

            Debug.Assert(data.ptr !=
                         null); // can't create dispose sentinel inside burst jobs, so array must be initialized prior to running jobs
            Debug.Assert(newAllocationLength > 0);
            Debug.Assert(data.length <= newAllocationLength);

            // add hysteresis to transition points
            // e.g. don't downsize to one below, this will avoid jitter when allocation constantly changing from 1024 to 2048 and back
            var needToGrow = newAllocationLength > data.allocationLength;
            var needToShrink = newAllocationLength * 2 < data.allocationLength;
            if (needToGrow || needToShrink && !growOnly)
            {
                var newPtr = UnsafeUtility.Malloc(newAllocationLength * UnsafeUtility.SizeOf<T>(),
                    UnsafeUtility.AlignOf<T>(), k_Label);
                UnsafeUtility.MemCpy(newPtr, data.ptr,
                    (oldLength < newLength ? oldLength : newLength) * UnsafeUtility.SizeOf<T>());
                UnsafeUtility.Free(data.ptr, k_Label);

                data.ptr = newPtr;
            }

            m_Data[0] = data;
        }

        public void Push(T value)
        {
            var length = m_Data[0].length;
            ResizeToFit(length + 1, true);
            UnsafeUtility.ArrayElementAsRef<T>(GetUnsafePtr(), length) = value;
        }

        public void ShrinkToFit()
        {
            ResizeToFit(m_Data[0].length);
        }

        private static int NextPowerOfTwo(int length)
        {
            // code from interwebs
            var v = (uint) length;
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return (int) v;
        }

        [BurstDiscard]
        public void Dispose()
        {
            if (m_Data.IsCreated)
            {
                var data = m_Data[0];

                Debug.Assert(data.ptr != null);
                UnsafeUtility.Free(data.ptr, k_Label);
                m_Data[0] = new Data
                {
                    ptr = null,
                    length = 0,
                    allocationLength = 0
                };
            }

            m_Data.Dispose();
        }

        // Warning! Pointer might change if array is resized 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            return m_Data[0].ptr;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle GetAtomicSafetyHandle()
        {
            return NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Data);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToNativeSlice()
        {
            var data = m_Data[0];

            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(data.ptr,
                UnsafeUtility.SizeOf<T>(),
                data.length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice,
                NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Data));
#endif
            return slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeNativeSlice<T> ToUnsafeNativeSlice()
        {
            return new UnsafeNativeSlice<T>(this);
        }
    }
}