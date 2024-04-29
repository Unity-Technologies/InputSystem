using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Experimental
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal unsafe struct NativeContext
    {
        static Stream* CreateStream(NativeContext* ctx, int capacity, long elementSize, int elementAlignment)
        {
            var stream = (Stream*)UnsafeUtility.Malloc(sizeof(Stream), UnsafeUtility.AlignOf<Stream>(), Allocator.Persistent);
            if (stream != null)
            {
                stream->data = UnsafeUtility.Malloc(capacity * elementSize, elementAlignment, Allocator.Persistent);
                stream->read_idx = 0;
                stream->write_idx = 0;
            }
            return stream;
        }

        static void DestroyStream(NativeContext* ctx, Stream* stream)
        {
            UnsafeUtility.Free(stream, Allocator.Persistent);
        }
    }
}