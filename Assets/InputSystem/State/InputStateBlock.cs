using System;
using ISX.Utilities;
using UnityEngine;

namespace ISX.LowLevel
{
    /// <summary>
    /// Information about a memory region storing state.
    /// </summary>
    /// <remarks>
    /// Input state is kept in raw memory blocks. All state is centrally managed by InputManager; controls
    /// cannot keep their own independent state. State can be used to store values received from external
    /// systems (input) or to accumulate values to send back to external systems (output).
    /// </remarks>
    public struct InputStateBlock
    {
        public const uint kInvalidOffset = 0xffffffff;

        // Primitive state type codes.
        public static FourCC kTypeBit = new FourCC('B', 'I', 'T');
        public static FourCC kTypeInt = new FourCC('I', 'N', 'T');
        public static FourCC kTypeShort = new FourCC('S', 'H', 'R', 'T');
        public static FourCC kTypeByte = new FourCC('B', 'Y', 'T', 'E');
        public static FourCC kTypeFloat = new FourCC('F', 'L', 'T');
        public static FourCC kTypeDouble = new FourCC('D', 'B', 'L');
        public static FourCC kTypeVector2 = new FourCC('V', 'E', 'C', '2');
        public static FourCC kTypeVector3 = new FourCC('V', 'E', 'C', '3');
        public static FourCC kTypeVector2Short = new FourCC('V', 'C', '2', 'S');
        public static FourCC kTypeVector3Short = new FourCC('V', 'C', '3', 'S');
        public static FourCC kTypeVector2Byte = new FourCC('V', 'C', '2', 'B');
        public static FourCC kTypeVector3Byte = new FourCC('V', 'C', '3', 'B');
        public static FourCC kTypeQuaternion = new FourCC('Q', 'U', 'A', 'T');

        public static int GetSizeOfPrimitiveFormatInBits(FourCC type)
        {
            if (type == kTypeBit)
                return 1;
            if (type == kTypeInt)
                return 4 * 8;
            if (type == kTypeShort)
                return 2 * 8;
            if (type == kTypeByte)
                return 1 * 8;
            if (type == kTypeFloat)
                return 4 * 8;
            if (type == kTypeDouble)
                return 8 * 8;
            if (type == kTypeVector2)
                return 2 * 4 * 8;
            if (type == kTypeVector3)
                return 3 * 4 * 8;
            if (type == kTypeQuaternion)
                return 4 * 4 * 8;
            if (type == kTypeVector2Short)
                return 2 * 2 * 8;
            if (type == kTypeVector3Short)
                return 3 * 2 * 8;
            if (type == kTypeVector2Byte)
                return 2 * 1 * 8;
            if (type == kTypeVector3Byte)
                return 3 * 1 * 8;
            return -1;
        }

        public static FourCC GetPrimitiveFormatFromType(Type type)
        {
            if (ReferenceEquals(type, typeof(int)))
                return kTypeInt;
            if (ReferenceEquals(type, typeof(short)))
                return kTypeShort;
            if (ReferenceEquals(type, typeof(byte)))
                return kTypeByte;
            if (ReferenceEquals(type, typeof(float)))
                return kTypeFloat;
            if (ReferenceEquals(type, typeof(double)))
                return kTypeDouble;
            if (ReferenceEquals(type, typeof(Vector2)))
                return kTypeVector2;
            if (ReferenceEquals(type, typeof(Vector3)))
                return kTypeVector3;
            if (ReferenceEquals(type, typeof(Quaternion)))
                return kTypeQuaternion;
            return new FourCC();
        }

        /// <summary>
        /// Type identifier for the memory layout used by the state.
        /// </summary>
        /// <remarks>
        /// Used for safety checks to make sure that when the system copies state memory, it
        /// copies between compatible layouts. If set to a primitive state format, also used to
        /// determine the size of the state block.
        /// </remarks>
        public FourCC format;

        // Offset into state buffer. After a device is added to the system, this is relative
        // to the global buffers; otherwise it is relative to the device root.
        // During setup, this can be kInvalidOffset to indicate a control that should be placed
        // at an offset automatically; otherwise it denotes a fixed offset relative to the
        // parent control.
        public uint byteOffset;

        // Bit offset from the given byte offset. Also zero-based (i.e. first bit is at bit
        // offset #0).
        public uint bitOffset;

        // Size of the state in bits. If this % 8 is not 0, the control is considered a
        // bitfield control.
        // During setup, if this field is 0 it means the size of the control should be automatically
        // computed from either its children (if it has any) or its set format. If it has neither,
        // setup will throw.
        public uint sizeInBits;

        public bool isBitfield
        {
            get { return sizeInBits % 8 != 0; }
        }

        internal uint alignedSizeInBytes
        {
            get { return (uint)((sizeInBits / 8) + (sizeInBits % 8 > 0 ? 1 : 0)); }
        }
    }
}
