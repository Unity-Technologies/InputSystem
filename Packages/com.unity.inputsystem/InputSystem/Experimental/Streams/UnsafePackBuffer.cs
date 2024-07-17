using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A utility that simplifies packing and unpacking data dynamically from an unsafe native byte buffer.
    /// </summary>
    /// <remarks>
    /// Note that this struct acts as a wrapper on top of the underlying buffer.
    ///
    /// Using this utility is NOT thread safe.
    /// </remarks>
    internal struct UnsafePackBuffer
    {
        //[NativeDisableUnsafePtrRestriction]
        private readonly unsafe void* m_Data;
        private readonly int m_Length;
        private int m_Offset;
        
        public unsafe UnsafePackBuffer([NotNull] void* data, int length)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (length < 0)
                throw new ArgumentException($"Argument {nameof(length)}");
            
            m_Data = data;
            m_Length = length;
            m_Offset = 0;
        }
        
        public int position
        {
            get => m_Offset;
            set
            {
                if (value < 0 || value >= m_Length)
                    throw new ArgumentOutOfRangeException($"nameof(position) must be zero or positive but was: {value}");
            
                m_Offset = value;
            }
        }

        public void Reset()
        {
            m_Offset = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(int alignOf)
        {
            CheckUlongPositivePowerOfTwo((ulong)alignOf);
            return MoveNextUnchecked(alignOf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext<T>() where T : struct
        {
            return MoveNextUnchecked(UnsafeUtility.AlignOf<T>());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MoveNextUnchecked(int alignOf)
        {
            m_Offset = CollectionHelper.Align(m_Offset, alignOf);
            return m_Offset < m_Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe T* GetPointer<T>() where T : unmanaged
        {
            return (T*)GetPointer(UnsafeUtility.AlignOf<T>());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void* GetPointer(int alignOf)
        {
            CheckBuffer();
            CheckPosition();
            var ptr = (void*)((IntPtr)m_Data + m_Offset);
            CheckAlignment(ptr, alignOf);
            return ptr;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write<T>(T value, int alignOf) where T : unmanaged
        {
            *(T*)GetPointer(alignOf) = value;
            m_Offset += sizeof(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value) where T : unmanaged => Write(value, UnsafeUtility.AlignOf<T>());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write<T>(ref T value, int alignOf) where T : unmanaged
        {
            *GetPointer<T>() = value;
            m_Offset += sizeof(T);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(ref T value) where T : unmanaged => Write(ref value, UnsafeUtility.AlignOf<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : unmanaged
        {
            return Read<T>(UnsafeUtility.AlignOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe T Read<T>(int alignOf) where T : unmanaged
        {
            var ptr = (T*)GetPointer(alignOf);
            m_Offset += sizeof(T);
            return *ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value) => Write(value, 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadShort() => Read<short>(2);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value) => Write(value, 2);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUnsignedShort() => Read<ushort>(2);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value) => Write(value, 4);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value) => Write(value, 4);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value) => Write(value, 8);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value) => Write(value, 8);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float value) => Write(value, 4);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double value) => Write(value, 8);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Vector2 value) => Write(value, 4);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Vector3 value) => Write(value, 4);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Quaternion value) => Write(value, 4);
        
        #region Checks
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_INPUT_SYSTEM_DEBUG")]
        private static void CheckUlongPositivePowerOfTwo(ulong value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException($"Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void CheckBuffer()
        {
            if (m_Data == null)
                throw new Exception("Buffer has not been initialized");
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void CheckPosition()
        {
            if (m_Offset >= m_Length)
                throw new Exception("Buffer capacity exceeded");
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CheckAlignment(void* ptr, int alignOf)
        {
            if (!CollectionHelper.IsAligned(ptr, alignOf))
                throw new ArgumentException($"Invalid memory alignment, did you forget to call {nameof(MoveNext)} with a valid alignment prior to invoking {nameof(Read)} or {nameof(Write)}?");
            
            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        #endregion
    }
}