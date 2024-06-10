using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    internal struct NativeLocalStream : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct NativeStreamSegment
        {
            public void* data;
            public uint length;
            public uint writeIndex;
            public uint readIndex;
            
            public int refCount;
            public NativeStreamSegment* next;
        }
        
        private unsafe NativeStreamSegment* m_Segment;
        private readonly Allocator m_Allocator;

        public unsafe NativeLocalStream(int capacity, int alignment, int elementSize, Allocator allocator)
        {
            m_Segment = AllocateSegment(capacity, alignment, elementSize, allocator);
            m_Allocator = allocator;
        }

        public unsafe void Release()
        {
            if (--m_Segment->refCount == 0)
                Destroy();
        }
        
        private unsafe void Destroy()
        {
            if (m_Segment == null)
                return;
            UnsafeUtility.Free(m_Segment, m_Allocator);
            m_Segment = null;
        }

        private static int AlignUp(int value, int alignment)
        {
            return value + (-value & (alignment - 1));
        }

        private static unsafe NativeStreamSegment* AllocateSegment(int capacity, int elementAlignment, int elementSize, Allocator allocator)
        {
            // Compute required allocation size taking both segment header and data alignment into account
            var headerAlignment = UnsafeUtility.AlignOf<NativeStreamSegment>();
            var headerSizeBytes = UnsafeUtility.SizeOf<NativeStreamSegment>();
            headerSizeBytes = AlignUp(headerSizeBytes, elementAlignment); 
            var totalSizeBytes = (capacity + 1) * elementSize + headerSizeBytes;
            
            // Allocate the required memory for segment + data
            var segment = (NativeStreamSegment*)UnsafeUtility.Malloc(totalSizeBytes, headerAlignment, allocator);
            UnsafeUtility.MemClear(segment, sizeof(NativeStreamSegment));
            segment->refCount = 1;
            return segment;
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}