using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal unsafe struct Stream
    {
        [FieldOffset(0)] public void* data;
        [FieldOffset(8)] public uint  read_idx;
        [FieldOffset(12)] public uint write_idx;
        [FieldOffset(16)] public ushort type;
    }

    internal unsafe struct WrappedStream
    {
        public WrappedStream(Stream* stream)
        {
            this.stream = stream;
        }
        
        public readonly Stream* stream;
    }
}