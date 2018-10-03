using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Utilities;

////TODO: the read/write methods need a proper pass for consistency

////FIXME: some architectures have strict memory alignment requirements; we should honor them when
////       we read/write primitive values or support stitching values together from bytes manually
////       where needed

////TODO: allow bitOffset to be non-zero for byte-aligned control as long as result is byte-aligned

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Information about a memory region storing state.
    /// </summary>
    /// <remarks>
    /// Input state is kept in raw memory blocks. All state is centrally managed by InputManager; controls
    /// cannot keep their own independent state.
    ///
    /// Each state block is tagged with a format code indicating the storage format used for the
    /// memory block. This can either be one out of a set of primitive formats (such as "INT") or a custom
    /// format code indicating a more complex format.
    ///
    /// Memory using primitive formats can be converted to and from primitive values directly by this struct.
    ///
    /// State memory is bit-addressable, meaning that it can be offset from a byte address in bits (<see cref="bitOffset"/>)
    /// and is sized in bits instead of bytes (<see cref="sizeInBits"/>). However, in practice, bit-addressing
    /// memory reads and writes are only supported on the <see cref="kTypeBit">bitfield primitive format</see>.
    ///
    /// Input state memory is restricted to a maximum of 4GB in size. Offsets are recorded in 32 bits.
    /// </remarks>
    public struct InputStateBlock
    {
        public const uint kInvalidOffset = 0xffffffff;

        // Primitive state type codes.
        public static FourCC kTypeBit = new FourCC('B', 'I', 'T');
        public static FourCC kTypeInt = new FourCC('I', 'N', 'T');
        public static FourCC kTypeUInt = new FourCC('U', 'I', 'N', 'T');
        public static FourCC kTypeShort = new FourCC('S', 'H', 'R', 'T');
        public static FourCC kTypeUShort = new FourCC('U', 'S', 'H', 'T');
        public static FourCC kTypeByte = new FourCC('B', 'Y', 'T', 'E');
        public static FourCC kTypeSByte = new FourCC('S', 'B', 'Y', 'T');
        public static FourCC kTypeLong = new FourCC('L', 'N', 'G');
        public static FourCC kTypeULong = new FourCC('U', 'L', 'N', 'G');
        public static FourCC kTypeFloat = new FourCC('F', 'L', 'T');
        public static FourCC kTypeDouble = new FourCC('D', 'B', 'L');

        ////REVIEW: are these really useful?
        public static FourCC kTypeVector2 = new FourCC('V', 'E', 'C', '2');
        public static FourCC kTypeVector3 = new FourCC('V', 'E', 'C', '3');
        public static FourCC kTypeQuaternion = new FourCC('Q', 'U', 'A', 'T');
        public static FourCC kTypeVector2Short = new FourCC('V', 'C', '2', 'S');
        public static FourCC kTypeVector3Short = new FourCC('V', 'C', '3', 'S');
        public static FourCC kTypeVector2Byte = new FourCC('V', 'C', '2', 'B');
        public static FourCC kTypeVector3Byte = new FourCC('V', 'C', '3', 'B');

        public static int GetSizeOfPrimitiveFormatInBits(FourCC type)
        {
            if (type == kTypeBit)
                return 1;
            if (type == kTypeInt || type == kTypeUInt)
                return 4 * 8;
            if (type == kTypeShort || type == kTypeUShort)
                return 2 * 8;
            if (type == kTypeByte || type == kTypeSByte)
                return 1 * 8;
            if (type == kTypeFloat)
                return 4 * 8;
            if (type == kTypeDouble)
                return 8 * 8;
            if (type == kTypeLong || type == kTypeULong)
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
            if (ReferenceEquals(type, typeof(uint)))
                return kTypeUInt;
            if (ReferenceEquals(type, typeof(short)))
                return kTypeShort;
            if (ReferenceEquals(type, typeof(ushort)))
                return kTypeUShort;
            if (ReferenceEquals(type, typeof(byte)))
                return kTypeByte;
            if (ReferenceEquals(type, typeof(sbyte)))
                return kTypeSByte;
            if (ReferenceEquals(type, typeof(float)))
                return kTypeFloat;
            if (ReferenceEquals(type, typeof(double)))
                return kTypeDouble;
            if (ReferenceEquals(type, typeof(long)))
                return kTypeLong;
            if (ReferenceEquals(type, typeof(ulong)))
                return kTypeULong;
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

        internal uint alignedSizeInBytes
        {
            get { return (uint)((sizeInBits / 8) + (sizeInBits % 8 > 0 ? 1 : 0)); }
        }

        public unsafe int ReadInt(IntPtr statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr.ToPointer() + (int)byteOffset;

            int value;
            if (format == kTypeInt || format == kTypeUInt)
            {
                Debug.Assert(sizeInBits == 32, "INT and UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT and UINT state must be byte-aligned");
                value = *(int*)valuePtr;
            }
            else if (format == kTypeBit)
            {
                if (sizeInBits == 1)
                    value = MemoryHelpers.ReadSingleBit(new IntPtr(valuePtr), bitOffset) ? 1 : 0;
                else
                    value = MemoryHelpers.ReadIntFromMultipleBits(new IntPtr(valuePtr), bitOffset, sizeInBits);
            }
            else if (format == kTypeByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                value = *valuePtr;
            }
            else if (format == kTypeSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                value = *(sbyte*)valuePtr;
            }
            else if (format == kTypeShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                value = *(short*)valuePtr;
            }
            else if (format == kTypeUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                value = *(ushort*)valuePtr;
            }
            else
            {
                throw new Exception(string.Format("State format '{0}' is not supported as integer format", format));
            }

            return value;
        }

        public unsafe void WriteInt(IntPtr statePtr, int value)
        {
            throw new NotImplementedException();
        }

        public unsafe float ReadFloat(IntPtr statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr.ToPointer() + (int)byteOffset;

            float value;
            if (format == kTypeFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                value = *(float*)valuePtr;
            }
            else if (format == kTypeBit)
            {
                if (sizeInBits != 1)
                    throw new NotImplementedException("Cannot yet convert multi-bit fields to floats");

                value = MemoryHelpers.ReadSingleBit(new IntPtr(valuePtr), bitOffset) ? 1.0f : 0.0f;
            }
            // If a control with an integer-based representation does not use the full range
            // of its integer size (e.g. only goes from [0..128]), processors or the parameters
            // above have to be used to re-process the resulting float values.
            else if (format == kTypeShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                ////REVIEW: What's better here? This code reaches a clean -1 but doesn't reach a clean +1 as the range is [-32768..32767].
                ////        Should we cut off at -32767? Or just live with the fact that 0.999 is as high as it gets?
                value = *((short*)valuePtr) / 32768.0f;
            }
            else if (format == kTypeUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                value = *((ushort*)valuePtr) / 65535.0f;
            }
            else if (format == kTypeByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                value = *valuePtr / 255.0f;
            }
            else if (format == kTypeSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                ////REVIEW: Same problem here as with 'short'
                value = *((sbyte*)valuePtr) / 128.0f;
            }
            else
            {
                throw new Exception(string.Format("State format '{0}' is not supported as floating-point format", format));
            }

            return value;
        }

        public unsafe void WriteFloat(IntPtr statePtr, float value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)byteOffset);

            if (format == kTypeFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                *(float*)valuePtr = value;
            }
            else if (format == kTypeBit)
            {
                if (sizeInBits != 1)
                    throw new NotImplementedException("Cannot yet convert multi-bit fields to floats");

                MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.5f);
            }
            else if (format == kTypeShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                *(short*)valuePtr = (short)(value * 32768.0f);
            }
            else if (format == kTypeUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                *(ushort*)valuePtr = (ushort)(value * 65535.0f);
            }
            else if (format == kTypeByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                *(byte*)valuePtr = (byte)(value * 255.0f);
            }
            else if (format == kTypeSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                *(sbyte*)valuePtr = (sbyte)(value * 128.0f);
            }
            else
            {
                throw new Exception(string.Format("State format '{0}' is not supported as floating-point format", format));
            }
        }

        public unsafe PrimitiveValue Read(IntPtr statePtr)
        {
            throw new NotImplementedException();
        }

        public unsafe void Write(IntPtr statePtr, PrimitiveValue value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)byteOffset);

            if (format == kTypeBit)
            {
                if (sizeInBits > 32)
                    throw new NotImplementedException(
                        "Cannot yet write primitive values into bitfields wider than 32 bits");

                if (sizeInBits == 1)
                    MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value.ToBool());
                else
                    MemoryHelpers.WriteIntFromMultipleBits(valuePtr, bitOffset, sizeInBits, value.ToInt());
            }
            else if (format == kTypeFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                *(float*)valuePtr = value.ToFloat();
            }
            else
            {
                throw new NotImplementedException(string.Format(
                    "Writing primitive value of type '{0}' into state block with format '{1}'", value.valueType,
                    format));
            }
        }

        public unsafe void CopyToFrom(IntPtr toStatePtr, IntPtr fromStatePtr)
        {
            if (bitOffset != 0 || sizeInBits % 8 != 0)
                throw new NotImplementedException("Copying bitfields");

            var from = (byte*)fromStatePtr.ToPointer() + byteOffset;
            var to = (byte*)toStatePtr.ToPointer() + byteOffset;

            UnsafeUtility.MemCpy(to, from, alignedSizeInBytes);
        }
    }
}
