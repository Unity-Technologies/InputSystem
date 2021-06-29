using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

namespace UnityEngine.InputSystem.DataPipeline.Collections
{
    // <summary>
    // A temporary workaround until System.Span is available. AlmostManagedSpan is lacking true index checks in release.
    // NativeSlice is not suitable because it injects index checks per element access, making it 20x slower compared to managed array.
    // </summary>
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(AlmostManagedSpanDebugView<>))]
    public unsafe struct AlmostManagedSpan<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        // TODO aliasing?
        public readonly void* Buffer;

        public readonly int Stride;
        public readonly int Length;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AlmostManagedSpan(void* buffer, int bufferLength, int spanOffset, int spanLength)
        {
            if (spanOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(spanOffset), string.Format("Span start {0} < 0.", spanOffset));
            if (spanLength < 0)
                throw new ArgumentOutOfRangeException(nameof(spanLength), string.Format("Span length {0} < 0.", spanLength));
            if (spanOffset + spanLength > bufferLength)
                throw new ArgumentException(string.Format("Span start + length ({0}) range must be <= array.Length ({1})", spanOffset + spanLength, bufferLength));
            if (spanOffset + spanLength < 0)
                throw new ArgumentException("Span start + length ({start + length}) causes an integer overflow");

            Stride = UnsafeUtility.SizeOf<T>();
            Buffer = (byte*) ((IntPtr) buffer + Stride * spanOffset);
            Length = spanLength;
        }
        
#if false // TODO add something like INPUT_PARANOID_CHECKS
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckIndex(int index)
        {
            if (index >= 0 && index < Length)
                return;

            throw new IndexOutOfRangeException(string.Format("Index {0} is out of range for '{1}' length.", index, Length));
        }
        

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckIndex(index);
                return UnsafeUtility.ReadArrayElementWithStride<T>(m_Buffer, index, Stride);
            }

            [WriteAccessRequired]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CheckIndex(index);
                UnsafeUtility.WriteArrayElementWithStride<T>(m_Buffer, index, Stride, value);
            }
        }

#else

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => UnsafeUtility.ReadArrayElementWithStride<T>(Buffer, index, Stride);

            [WriteAccessRequired]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => UnsafeUtility.WriteArrayElementWithStride<T>(Buffer, index, Stride, value);
        }

#endif

        public T[] ToArray()
        {
            var array = new T[Length];

            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            
            UnsafeUtility.MemCpyStride(
                (void*) handle.AddrOfPinnedObject(),
                UnsafeUtility.SizeOf<T>(),
                Buffer,
                Stride,
                UnsafeUtility.SizeOf<T>(),
                Length);

            handle.Free();

            return array;
        }
    }
}