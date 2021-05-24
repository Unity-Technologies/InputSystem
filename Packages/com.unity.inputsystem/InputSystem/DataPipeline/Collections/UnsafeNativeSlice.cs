using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DataPipeline.Collections
{
    // Can't use NativeSlice because DatasetProxy is passed around as an argument, so it has to be a non-managed type.
    // TODO move to collections proper
    public unsafe struct UnsafeNativeSlice<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction] [NoAlias]
        public void* ptr;

        public int length;

        public int stride;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeNativeSlice(NativeArray<T> array)
        {
            ptr = array.GetUnsafePtr();
            length = array.Length;
            stride = UnsafeUtility.SizeOf<T>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeNativeSlice(ResizableNativeArray<T> array)
        {
            ptr = array.GetUnsafePtr();
            length = array.Length;
            stride = UnsafeUtility.SizeOf<T>();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = array.GetAtomicSafetyHandle();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToNativeSlice()
        {
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(ptr, stride, length);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, m_Safety);
#endif
            return slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<T> ToNativeSlice(int offsetInItems, int lengthsInItems)
        {
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(
                (byte*) ptr + stride * offsetInItems, stride, lengthsInItems);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, m_Safety);
#endif
            return slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TOther> ToNativeSlice<TOther>(int offsetInBytes, int itemStride, int lengthsInItems)
            where TOther : struct
        {
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<TOther>((byte*) ptr + offsetInBytes,
                itemStride, lengthsInItems);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, m_Safety);
#endif
            return slice;
        }
    }

    public static class UnsafeNativeSliceExtensions
    {
        public static UnsafeNativeSlice<T> ToUnsafeNativeSlice<T>(this NativeArray<T> array) where T : struct
        {
            return new UnsafeNativeSlice<T>(array);
        }
    }
}