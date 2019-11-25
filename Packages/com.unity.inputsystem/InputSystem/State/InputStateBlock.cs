using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

////TODO: the Debug.Asserts here should be also be made as checks ahead of time (on the layout)

////TODO: the read/write methods need a proper pass for consistency

////FIXME: some architectures have strict memory alignment requirements; we should honor them when
////       we read/write primitive values or support stitching values together from bytes manually
////       where needed

////TODO: allow bitOffset to be non-zero for byte-aligned control as long as result is byte-aligned

////REVIEW: The combination of byte and bit offset instead of just a single bit offset has turned out
////        to be plenty awkward to use in practice; should be replace it?

////REVIEW: AutomaticOffset is a very awkward mechanism; it's primary use really is for "parking" unused
////        controls for which a more elegant and robust mechanism can surely be devised

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Information about a memory region storing input state.
    /// </summary>
    /// <remarks>
    /// Input state is kept in raw memory blocks. All state is centrally managed by the input system;
    /// controls cannot keep their own independent state.
    ///
    /// Each state block is tagged with a format code indicating the storage format used for the
    /// memory block. This can either be one out of a set of primitive formats (such as "INT") or a custom
    /// format code indicating a more complex format.
    ///
    /// Memory using primitive formats can be converted to and from primitive values directly by this struct.
    ///
    /// State memory is bit-addressable, meaning that it can be offset from a byte address in bits (<see cref="bitOffset"/>)
    /// and is sized in bits instead of bytes (<see cref="sizeInBits"/>). However, in practice, bit-addressing
    /// memory reads and writes are only supported on the <see cref="FormatBit">bitfield primitive format</see>.
    ///
    /// Input state memory is restricted to a maximum of 4GB in size. Offsets are recorded in 32 bits.
    /// </remarks>
    /// <seealso cref="InputControl.stateBlock"/>
    public unsafe struct InputStateBlock
    {
        public const uint InvalidOffset = 0xffffffff;
        public const uint AutomaticOffset = 0xfffffffe;

        /// <summary>
        /// Format code for a variable-width bitfield representing an unsigned value,
        /// i.e. all bits including the highest one represent the magnitude of the value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatBit = new FourCC('B', 'I', 'T');

        /// <summary>
        /// Format code for a variable-width bitfield representing a signed value, i.e. the
        /// highest bit is used as a sign bit (0=unsigned, 1=signed) and the remaining bits represent
        /// the magnitude of the value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatSBit = new FourCC('S', 'B', 'I', 'T');

        /// <summary>
        /// Format code for a 32-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatInt = new FourCC('I', 'N', 'T');

        /// <summary>
        /// Format code for a 32-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatUInt = new FourCC('U', 'I', 'N', 'T');

        /// <summary>
        /// Format code for a 16-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatShort = new FourCC('S', 'H', 'R', 'T');

        /// <summary>
        /// Format code for a 16-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatUShort = new FourCC('U', 'S', 'H', 'T');

        /// <summary>
        /// Format code for an 8-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatByte = new FourCC('B', 'Y', 'T', 'E');

        /// <summary>
        /// Format code for an 8-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatSByte = new FourCC('S', 'B', 'Y', 'T');

        /// <summary>
        /// Format code for a 64-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatLong = new FourCC('L', 'N', 'G');

        /// <summary>
        /// Format code for a 64-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatULong = new FourCC('U', 'L', 'N', 'G');

        /// <summary>
        /// Format code for a 32-bit floating-point value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatFloat = new FourCC('F', 'L', 'T');

        /// <summary>
        /// Format code for a 64-bit floating-point value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatDouble = new FourCC('D', 'B', 'L');

        ////REVIEW: are these really useful?
        public static readonly FourCC FormatVector2 = new FourCC('V', 'E', 'C', '2');
        public static readonly FourCC FormatVector3 = new FourCC('V', 'E', 'C', '3');
        public static readonly FourCC FormatQuaternion = new FourCC('Q', 'U', 'A', 'T');
        public static readonly FourCC FormatVector2Short = new FourCC('V', 'C', '2', 'S');
        public static readonly FourCC FormatVector3Short = new FourCC('V', 'C', '3', 'S');
        public static readonly FourCC FormatVector2Byte = new FourCC('V', 'C', '2', 'B');
        public static readonly FourCC FormatVector3Byte = new FourCC('V', 'C', '3', 'B');

        public static int GetSizeOfPrimitiveFormatInBits(FourCC type)
        {
            if (type == FormatBit || type == FormatSBit)
                return 1;
            if (type == FormatInt || type == FormatUInt)
                return 4 * 8;
            if (type == FormatShort || type == FormatUShort)
                return 2 * 8;
            if (type == FormatByte || type == FormatSByte)
                return 1 * 8;
            if (type == FormatFloat)
                return 4 * 8;
            if (type == FormatDouble)
                return 8 * 8;
            if (type == FormatLong || type == FormatULong)
                return 8 * 8;
            if (type == FormatVector2)
                return 2 * 4 * 8;
            if (type == FormatVector3)
                return 3 * 4 * 8;
            if (type == FormatQuaternion)
                return 4 * 4 * 8;
            if (type == FormatVector2Short)
                return 2 * 2 * 8;
            if (type == FormatVector3Short)
                return 3 * 2 * 8;
            if (type == FormatVector2Byte)
                return 2 * 1 * 8;
            if (type == FormatVector3Byte)
                return 3 * 1 * 8;
            return -1;
        }

        public static FourCC GetPrimitiveFormatFromType(Type type)
        {
            if (ReferenceEquals(type, typeof(int)))
                return FormatInt;
            if (ReferenceEquals(type, typeof(uint)))
                return FormatUInt;
            if (ReferenceEquals(type, typeof(short)))
                return FormatShort;
            if (ReferenceEquals(type, typeof(ushort)))
                return FormatUShort;
            if (ReferenceEquals(type, typeof(byte)))
                return FormatByte;
            if (ReferenceEquals(type, typeof(sbyte)))
                return FormatSByte;
            if (ReferenceEquals(type, typeof(float)))
                return FormatFloat;
            if (ReferenceEquals(type, typeof(double)))
                return FormatDouble;
            if (ReferenceEquals(type, typeof(long)))
                return FormatLong;
            if (ReferenceEquals(type, typeof(ulong)))
                return FormatULong;
            if (ReferenceEquals(type, typeof(Vector2)))
                return FormatVector2;
            if (ReferenceEquals(type, typeof(Vector3)))
                return FormatVector3;
            if (ReferenceEquals(type, typeof(Quaternion)))
                return FormatQuaternion;
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
        public FourCC format { get; set; }

        // Offset into state buffer. After a device is added to the system, this is relative
        // to the global buffers; otherwise it is relative to the device root.
        // During setup, this can be InvalidOffset to indicate a control that should be placed
        // at an offset automatically; otherwise it denotes a fixed offset relative to the
        // parent control.
        public uint byteOffset { get; set; }

        // Bit offset from the given byte offset. Also zero-based (i.e. first bit is at bit
        // offset #0).
        public uint bitOffset { get; set; }

        // Size of the state in bits. If this % 8 is not 0, the control is considered a
        // bitfield control.
        // During setup, if this field is 0 it means the size of the control should be automatically
        // computed from either its children (if it has any) or its set format. If it has neither,
        // setup will throw.
        public uint sizeInBits { get; set; }

        internal uint alignedSizeInBytes => (sizeInBits + 7) >> 3;

        public int ReadInt(void* statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            int value;
            if (format == FormatInt || format == FormatUInt)
            {
                Debug.Assert(sizeInBits == 32, "INT and UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT and UINT state must be byte-aligned");
                value = *(int*)valuePtr;
            }
            else if (format == FormatBit)
            {
                if (sizeInBits == 1)
                    value = MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1 : 0;
                else
                    value = MemoryHelpers.ReadIntFromMultipleBits(valuePtr, bitOffset, sizeInBits);
            }
            else if (format == FormatSBit)
            {
                if (sizeInBits == 1)
                {
                    value = MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1 : -1;
                }
                else
                {
                    var halfMax = (1 << (int)sizeInBits) / 2;
                    var unsignedValue = MemoryHelpers.ReadIntFromMultipleBits(valuePtr, bitOffset, sizeInBits);
                    value = unsignedValue - halfMax;
                }
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                value = *valuePtr;
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                value = *(sbyte*)valuePtr;
            }
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                value = *(short*)valuePtr;
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                value = *(ushort*)valuePtr;
            }
            else
            {
                throw new InvalidOperationException($"State format '{format}' is not supported as integer format");
            }

            return value;
        }

        public void WriteInt(void* statePtr, int value)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            if (format == FormatInt || format == FormatUInt)
            {
                Debug.Assert(sizeInBits == 32, "INT and UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT and UINT state must be byte-aligned");
                *(int*)valuePtr = value;
            }
            else if (format == FormatBit)
            {
                if (sizeInBits == 1)
                    MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value != 0);
                else
                    MemoryHelpers.WriteIntFromMultipleBits(valuePtr, bitOffset, sizeInBits, value);
            }
            else if (format == FormatSBit)
            {
                if (sizeInBits == 1)
                {
                    MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value > 0);
                }
                else
                {
                    var halfMax = (1 << (int)sizeInBits) / 2;
                    MemoryHelpers.WriteIntFromMultipleBits(valuePtr, bitOffset, sizeInBits, value + halfMax);
                }
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                *valuePtr = (byte)value;
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                *(sbyte*)valuePtr = (sbyte)value;
            }
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                *(short*)valuePtr = (short)value;
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                *(ushort*)valuePtr = (ushort)value;
            }
            else
            {
                throw new Exception($"State format '{format}' is not supported as integer format");
            }
        }

        public float ReadFloat(void* statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            float value;
            if (format == FormatFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                value = *(float*)valuePtr;
            }
            else if (format == FormatBit || format == FormatSBit)
            {
                if (sizeInBits == 1)
                {
                    value = MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1.0f : (format == FormatSBit ? -1.0f : 0.0f);
                }
                else if (sizeInBits <= 31)
                {
                    var maxValue = (float)(1 << (int)sizeInBits);
                    var rawValue = (float)(MemoryHelpers.ReadIntFromMultipleBits(valuePtr, bitOffset, sizeInBits));
                    if (format == FormatSBit)
                    {
                        var unclampedValue = (rawValue / maxValue) * 2.0f - 1.0f;
                        value = Mathf.Clamp(unclampedValue, -1.0f, 1.0f);
                    }
                    else
                    {
                        value = Mathf.Clamp(rawValue / maxValue, 0.0f, 1.0f);
                    }
                }
                else
                {
                    throw new NotImplementedException("Cannot yet convert multi-bit fields greater than 31 bits to floats");
                }
            }
            // If a control with an integer-based representation does not use the full range
            // of its integer size (e.g. only goes from [0..128]), processors or the parameters
            // above have to be used to re-process the resulting float values.
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                ////REVIEW: What's better here? This code reaches a clean -1 but doesn't reach a clean +1 as the range is [-32768..32767].
                ////        Should we cut off at -32767? Or just live with the fact that 0.999 is as high as it gets?
                value = *(short*)valuePtr / 32768.0f;
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                value = *(ushort*)valuePtr / 65535.0f;
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                value = *valuePtr / 255.0f;
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                ////REVIEW: Same problem here as with 'short'
                value = *(sbyte*)valuePtr / 128.0f;
            }
            else if (format == FormatInt)
            {
                Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                value = *(int*)valuePtr / 2147483647.0f;
            }
            else if (format == FormatUInt)
            {
                Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                value = *(uint*)valuePtr / 4294967295.0f;
            }
            else if (format == FormatDouble)
            {
                Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                value = (float)*(double*)valuePtr;
            }
            else
            {
                throw new InvalidOperationException($"State format '{format}' is not supported as floating-point format");
            }

            return value;
        }

        public void WriteFloat(void* statePtr, float value)
        {
            var valuePtr = (byte*)statePtr + (int)byteOffset;

            if (format == FormatFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                *(float*)valuePtr = value;
            }
            else if (format == FormatBit)
            {
                if (sizeInBits == 1)
                {
                    MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.5f);
                }
                else
                {
                    var maxValue = (1 << (int)sizeInBits) - 1;
                    var intValue = (int)(value * maxValue);
                    MemoryHelpers.WriteIntFromMultipleBits(valuePtr, bitOffset, sizeInBits, intValue);
                }
            }
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                *(short*)valuePtr = (short)(value * 32768.0f);
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                *(ushort*)valuePtr = (ushort)(value * 65535.0f);
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                *valuePtr = (byte)(value * 255.0f);
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                *(sbyte*)valuePtr = (sbyte)(value * 128.0f);
            }
            else if (format == FormatDouble)
            {
                Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                *(double*)valuePtr = value;
            }
            else
            {
                throw new Exception($"State format '{format}' is not supported as floating-point format");
            }
        }

        internal PrimitiveValue FloatToPrimitiveValue(float value)
        {
            if (format == FormatFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                return value;
            }
            else if (format == FormatBit)
            {
                if (sizeInBits == 1)
                {
                    return value >= 0.5f;
                }
                else
                {
                    var maxValue = (1 << (int)sizeInBits) - 1;
                    return (int)(value * maxValue);
                }
            }
            else if (format == FormatInt)
            {
                Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                return (int)(value * 2147483647.0f);
            }
            else if (format == FormatUInt)
            {
                Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                return (uint)(value * 4294967295.0f);
            }
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                return (short)(value * 32768.0f);
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                return (ushort)(value * 65535.0f);
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                return (byte)(value * 255.0f);
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                return (sbyte)(value * 128.0f);
            }
            else if (format == FormatDouble)
            {
                Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                return value;
            }
            else
            {
                throw new Exception($"State format '{format}' is not supported as floating-point format");
            }
        }

        ////REVIEW: This is some bad code duplication here between Read/WriteFloat&Double but given that there's no
        ////        way to use a type argument here, not sure how to get rid of it.

        public double ReadDouble(void* statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            double value;
            if (format == FormatFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                value = *(float*)valuePtr;
            }
            else if (format == FormatBit || format == FormatSBit)
            {
                if (sizeInBits == 1)
                {
                    value = MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1.0f : (format == FormatSBit ? -1.0f : 0.0f);
                }
                else if (sizeInBits != 31)
                {
                    var maxValue = (float)(1 << (int)sizeInBits);
                    var rawValue = (float)(MemoryHelpers.ReadIntFromMultipleBits(valuePtr, bitOffset, sizeInBits));
                    if (format == FormatSBit)
                    {
                        var unclampedValue = (((rawValue / maxValue) * 2.0f) - 1.0f);
                        value = Mathf.Clamp(unclampedValue, -1.0f, 1.0f);
                    }
                    else
                    {
                        value = Mathf.Clamp(rawValue / maxValue, 0.0f, 1.0f);
                    }
                }
                else
                {
                    throw new NotImplementedException("Cannot yet convert multi-bit fields greater than 31 bits to floats");
                }
            }
            // If a control with an integer-based representation does not use the full range
            // of its integer size (e.g. only goes from [0..128]), processors or the parameters
            // above have to be used to re-process the resulting float values.
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                ////REVIEW: What's better here? This code reaches a clean -1 but doesn't reach a clean +1 as the range is [-32768..32767].
                ////        Should we cut off at -32767? Or just live with the fact that 0.999 is as high as it gets?
                value = *(short*)valuePtr / 32768.0f;
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                value = *(ushort*)valuePtr / 65535.0f;
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                value = *valuePtr / 255.0f;
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                ////REVIEW: Same problem here as with 'short'
                value = *(sbyte*)valuePtr / 128.0f;
            }
            else if (format == FormatInt)
            {
                Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                value = *(Int32*)valuePtr / 2147483647.0f;
            }
            else if (format == FormatUInt)
            {
                Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                value = *(UInt32*)valuePtr / 4294967295.0f;
            }
            else if (format == FormatDouble)
            {
                Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                value = *(double*)valuePtr;
            }
            else
            {
                throw new Exception($"State format '{format}' is not supported as floating-point format");
            }

            return value;
        }

        public void WriteDouble(void* statePtr, double value)
        {
            var valuePtr = (byte*)statePtr + (int)byteOffset;

            if (format == FormatFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                *(float*)valuePtr = (float)value;
            }
            else if (format == FormatBit)
            {
                if (sizeInBits == 1)
                {
                    MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.5f);
                }
                else
                {
                    var maxValue = (1 << (int)sizeInBits) - 1;
                    var intValue = (int)(value * maxValue);
                    MemoryHelpers.WriteIntFromMultipleBits(valuePtr, bitOffset, sizeInBits, intValue);
                }
            }
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                *(short*)valuePtr = (short)(value * 32768.0f);
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                *(ushort*)valuePtr = (ushort)(value * 65535.0f);
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                *valuePtr = (byte)(value * 255.0f);
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                *(sbyte*)valuePtr = (sbyte)(value * 128.0f);
            }
            else if (format == FormatDouble)
            {
                Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                *(double*)valuePtr = value;
            }
            else
            {
                throw new InvalidOperationException($"State format '{format}' is not supported as floating-point format");
            }
        }

        public void Write(void* statePtr, PrimitiveValue value)
        {
            var valuePtr = (byte*)statePtr + (int)byteOffset;

            if (format == FormatBit || format == FormatSBit)
            {
                if (sizeInBits > 32)
                    throw new NotImplementedException(
                        "Cannot yet write primitive values into bitfields wider than 32 bits");

                if (sizeInBits == 1)
                    MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value.ToBoolean());
                else
                    MemoryHelpers.WriteIntFromMultipleBits(valuePtr, bitOffset, sizeInBits, value.ToInt32());
            }
            else if (format == FormatFloat)
            {
                Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                *(float*)valuePtr = value.ToSingle();
            }
            else if (format == FormatByte)
            {
                Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                *valuePtr = value.ToByte();
            }
            else if (format == FormatShort)
            {
                Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                *(short*)valuePtr = value.ToInt16();
            }
            else if (format == FormatInt)
            {
                Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                *(int*)valuePtr = value.ToInt32();
            }
            else if (format == FormatSByte)
            {
                Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                *(sbyte*)valuePtr = value.ToSByte();
            }
            else if (format == FormatUShort)
            {
                Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                *(ushort*)valuePtr = value.ToUInt16();
            }
            else if (format == FormatUInt)
            {
                Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                *(uint*)valuePtr = value.ToUInt32();
            }
            else
            {
                throw new NotImplementedException(
                    $"Writing primitive value of type '{value.type}' into state block with format '{format}'");
            }
        }

        public void CopyToFrom(void* toStatePtr, void* fromStatePtr)
        {
            if (bitOffset != 0 || sizeInBits % 8 != 0)
                throw new NotImplementedException("Copying bitfields");

            var from = (byte*)fromStatePtr + byteOffset;
            var to = (byte*)toStatePtr + byteOffset;

            UnsafeUtility.MemCpy(to, from, alignedSizeInBytes);
        }
    }
}
