#if UNITY_2018_2
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.Utilities
{
    internal static class Unity2018_2_Compatibility
    {
        // NativeArray.Copy was added in 2018.3.
        public static unsafe void Copy<T>(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
            where T : struct
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "length must be equal or greater than zero.");
            if (srcIndex < 0 || srcIndex > src.Length || srcIndex == src.Length && src.Length > 0)
                throw new ArgumentOutOfRangeException("srcIndex", "srcIndex is outside the range of valid indexes for the source NativeArray.");
            if (dstIndex < 0 || dstIndex > dst.Length || dstIndex == dst.Length && dst.Length > 0)
                throw new ArgumentOutOfRangeException("dstIndex", "dstIndex is outside the range of valid indexes for the destination NativeArray.");
            if (srcIndex + length > src.Length)
                throw new ArgumentException(
                    "length is greater than the number of elements from srcIndex to the end of the source NativeArray.",
                    "length");
            if (dstIndex + length > dst.Length)
                throw new ArgumentException(
                    "length is greater than the number of elements from dstIndex to the end of the destination NativeArray.",
                    "length");

            UnsafeUtility.MemCpy((byte*)dst.GetUnsafePtr() + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.GetUnsafeReadOnlyPtr() + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());
        }
    }
}
#endif
