using System;
using System.Runtime.CompilerServices;
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
        /// Format code for invalid value type
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatInvalid = new FourCC(0);
        internal const int kFormatInvalid = 0;

        /// <summary>
        /// Format code for a variable-width bitfield representing an unsigned value,
        /// i.e. all bits including the highest one represent the magnitude of the value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatBit = new FourCC('B', 'I', 'T');
        internal const int kFormatBit = 'B' << 24 | 'I' << 16 | 'T' << 8 | ' ';

        /// <summary>
        /// Format code for a variable-width bitfield representing a signed value, i.e. the
        /// highest bit is used as a sign bit (0=unsigned, 1=signed) and the remaining bits represent
        /// the magnitude of the value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatSBit = new FourCC('S', 'B', 'I', 'T');
        internal const int kFormatSBit = 'S' << 24 | 'B' << 16 | 'I' << 8 | 'T';

        /// <summary>
        /// Format code for a 32-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatInt = new FourCC('I', 'N', 'T');
        internal const int kFormatInt = 'I' << 24 | 'N' << 16 | 'T' << 8 | ' ';

        /// <summary>
        /// Format code for a 32-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatUInt = new FourCC('U', 'I', 'N', 'T');
        internal const int kFormatUInt = 'U' << 24 | 'I' << 16 | 'N' << 8 | 'T';

        /// <summary>
        /// Format code for a 16-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatShort = new FourCC('S', 'H', 'R', 'T');
        internal const int kFormatShort = 'S' << 24 | 'H' << 16 | 'R' << 8 | 'T';

        /// <summary>
        /// Format code for a 16-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatUShort = new FourCC('U', 'S', 'H', 'T');
        internal const int kFormatUShort = 'U' << 24 | 'S' << 16 | 'H' << 8 | 'T';

        /// <summary>
        /// Format code for an 8-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatByte = new FourCC('B', 'Y', 'T', 'E');
        internal const int kFormatByte = 'B' << 24 | 'Y' << 16 | 'T' << 8 | 'E';

        /// <summary>
        /// Format code for an 8-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatSByte = new FourCC('S', 'B', 'Y', 'T');
        internal const int kFormatSByte = 'S' << 24 | 'B' << 16 | 'Y' << 8 | 'T';

        /// <summary>
        /// Format code for a 64-bit signed integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatLong = new FourCC('L', 'N', 'G');
        internal const int kFormatLong = 'L' << 24 | 'N' << 16 | 'G' << 8 | ' ';

        /// <summary>
        /// Format code for a 64-bit unsigned integer value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatULong = new FourCC('U', 'L', 'N', 'G');
        internal const int kFormatULong = 'U' << 24 | 'L' << 16 | 'N' << 8 | 'G';

        /// <summary>
        /// Format code for a 32-bit floating-point value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatFloat = new FourCC('F', 'L', 'T');
        internal const int kFormatFloat = 'F' << 24 | 'L' << 16 | 'T' << 8 | ' ';

        /// <summary>
        /// Format code for a 64-bit floating-point value.
        /// </summary>
        /// <seealso cref="format"/>
        public static readonly FourCC FormatDouble = new FourCC('D', 'B', 'L');
        internal const int kFormatDouble = 'D' << 24 | 'B' << 16 | 'L' << 8 | ' ';

        ////REVIEW: are these really useful?
        public static readonly FourCC FormatVector2 = new FourCC('V', 'E', 'C', '2');
        internal const int kFormatVector2 = 'V' << 24 | 'E' << 16 | 'C' << 8 | '2';
        public static readonly FourCC FormatVector3 = new FourCC('V', 'E', 'C', '3');
        internal const int kFormatVector3 = 'V' << 24 | 'E' << 16 | 'C' << 8 | '3';
        public static readonly FourCC FormatQuaternion = new FourCC('Q', 'U', 'A', 'T');
        internal const int kFormatQuaternion = 'Q' << 24 | 'U' << 16 | 'A' << 8 | 'T';
        public static readonly FourCC FormatVector2Short = new FourCC('V', 'C', '2', 'S');
        public static readonly FourCC FormatVector3Short = new FourCC('V', 'C', '3', 'S');
        public static readonly FourCC FormatVector2Byte = new FourCC('V', 'C', '2', 'B');
        public static readonly FourCC FormatVector3Byte = new FourCC('V', 'C', '3', 'B');
        public static readonly FourCC FormatPose = new FourCC('P', 'o', 's', 'e');
        internal const int kFormatPose = 'P' << 24 | 'o' << 16 | 's' << 8 | 'e';

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
            if (type == FormatLong || type == FormatULong)
                return 8 * 8;
            if (type == FormatFloat)
                return 4 * 8;
            if (type == FormatDouble)
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
            if (ReferenceEquals(type, typeof(long)))
                return FormatLong;
            if (ReferenceEquals(type, typeof(ulong)))
                return FormatULong;
            if (ReferenceEquals(type, typeof(float)))
                return FormatFloat;
            if (ReferenceEquals(type, typeof(double)))
                return FormatDouble;
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

        ////TODO: collapse byteOffset and bitOffset into a single 'offset' field
        // Offset into state buffer. After a device is added to the system, this is relative
        // to the global buffers; otherwise it is relative to the device root.
        // During setup, this can be InvalidOffset to indicate a control that should be placed
        // at an offset automatically; otherwise it denotes a fixed offset relative to the
        // parent control.
        public uint byteOffset
        {
            get => m_ByteOffset;
            set
            {
                m_ByteOffset = value;
            }
        }

        // Needed for fast access to avoid a call to getter in some places
        internal uint m_ByteOffset;

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
        internal uint effectiveByteOffset => byteOffset + (bitOffset >> 3);
        internal uint effectiveBitOffset => byteOffset * 8 + bitOffset;

        public int ReadInt(void* statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                case kFormatBit:
                    if (sizeInBits == 1)
                        return MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1 : 0;
                    return (int)MemoryHelpers.ReadMultipleBitsAsUInt(valuePtr, bitOffset, sizeInBits);
                case kFormatSBit:
                    if (sizeInBits == 1)
                        return MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1 : -1;
                    return MemoryHelpers.ReadExcessKMultipleBitsAsInt(valuePtr, bitOffset, sizeInBits);
                case kFormatInt:
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "INT and UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT and UINT state must be byte-aligned");
                    if (fmt == kFormatUInt)
                        Debug.Assert(*(uint*)valuePtr <= int.MaxValue, "UINT must fit in the int");
                    return *(int*)valuePtr;
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    return *(short*)valuePtr;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    return *(ushort*)valuePtr;
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    return *valuePtr;
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    return *(sbyte*)valuePtr;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                // - kFormatFloat
                // - kFormatDouble
                default:
                    throw new InvalidOperationException($"State format '{format}' is not supported as integer format");
            }
        }

        public void WriteInt(void* statePtr, int value)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                case kFormatBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value != 0);
                    else
                        MemoryHelpers.WriteUIntAsMultipleBits(valuePtr, bitOffset, sizeInBits, (uint)value);
                    break;
                case kFormatSBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value > 0);
                    else
                        MemoryHelpers.WriteIntAsExcessKMultipleBits(valuePtr, bitOffset, sizeInBits, value);
                    break;
                case kFormatInt:
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "INT and UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT and UINT state must be byte-aligned");
                    *(int*)valuePtr = value;
                    break;
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    *(short*)valuePtr = (short)value;
                    break;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    *(ushort*)valuePtr = (ushort)value;
                    break;
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    *valuePtr = (byte)value;
                    break;
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    *(sbyte*)valuePtr = (sbyte)value;
                    break;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                // - kFormatFloat
                // - kFormatDouble
                default:
                    throw new Exception($"State format '{format}' is not supported as integer format");
            }
        }

        public float ReadFloat(void* statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                // If a control with an integer-based representation does not use the full range
                // of its integer size (e.g. only goes from [0..128]), processors or the parameters
                // above have to be used to re-process the resulting float values.
                case kFormatBit:
                    if (sizeInBits == 1)
                        return MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1.0f : 0.0f;
                    return MemoryHelpers.ReadMultipleBitsAsNormalizedUInt(valuePtr, bitOffset, sizeInBits);
                case kFormatSBit:
                    if (sizeInBits == 1)
                        return MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1.0f : -1.0f;
                    return MemoryHelpers.ReadMultipleBitsAsNormalizedUInt(valuePtr, bitOffset, sizeInBits) * 2.0f - 1.0f;
                case kFormatInt:
                    Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                    return NumberHelpers.IntToNormalizedFloat(*(int*)valuePtr, int.MinValue, int.MaxValue) * 2.0f - 1.0f;
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                    return NumberHelpers.UIntToNormalizedFloat(*(uint*)valuePtr, uint.MinValue, uint.MaxValue);
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    return NumberHelpers.IntToNormalizedFloat(*(short*)valuePtr, short.MinValue, short.MaxValue) * 2.0f - 1.0f;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    return NumberHelpers.UIntToNormalizedFloat(*(ushort*)valuePtr, ushort.MinValue, ushort.MaxValue);
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    return NumberHelpers.UIntToNormalizedFloat(*valuePtr, byte.MinValue, byte.MaxValue);
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    return NumberHelpers.IntToNormalizedFloat(*(sbyte*)valuePtr, sbyte.MinValue, sbyte.MaxValue) * 2.0f - 1.0f;
                case kFormatFloat:
                    Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                    return *(float*)valuePtr;
                case kFormatDouble:
                    Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                    Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                    return (float)*(double*)valuePtr;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                default:
                    throw new InvalidOperationException($"State format '{format}' is not supported as floating-point format");
            }
        }

        public void WriteFloat(void* statePtr, float value)
        {
            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                case kFormatBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.5f);////REVIEW: Shouldn't this be the global button press point?
                    else
                        MemoryHelpers.WriteNormalizedUIntAsMultipleBits(valuePtr, bitOffset, sizeInBits, value);
                    break;
                case kFormatSBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.0f);
                    else
                        MemoryHelpers.WriteNormalizedUIntAsMultipleBits(valuePtr, bitOffset, sizeInBits, value * 0.5f + 0.5f);
                    break;
                case kFormatInt:
                    Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                    *(int*)valuePtr = (int)NumberHelpers.NormalizedFloatToInt(value * 0.5f + 0.5f, int.MinValue, int.MaxValue);
                    break;
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                    *(uint*)valuePtr = NumberHelpers.NormalizedFloatToUInt(value, uint.MinValue, uint.MaxValue);
                    break;
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    *(short*)valuePtr = (short)NumberHelpers.NormalizedFloatToInt(value * 0.5f + 0.5f, short.MinValue, short.MaxValue);
                    break;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    *(ushort*)valuePtr = (ushort)NumberHelpers.NormalizedFloatToUInt(value, ushort.MinValue, ushort.MaxValue);
                    break;
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    *valuePtr = (byte)NumberHelpers.NormalizedFloatToUInt(value, byte.MinValue, byte.MaxValue);
                    break;
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    *(sbyte*)valuePtr = (sbyte)NumberHelpers.NormalizedFloatToInt(value * 0.5f + 0.5f, sbyte.MinValue, sbyte.MaxValue);
                    break;
                case kFormatFloat:
                    Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                    *(float*)valuePtr = value;
                    break;
                case kFormatDouble:
                    Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                    Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                    *(double*)valuePtr = value;
                    break;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                default:
                    throw new Exception($"State format '{format}' is not supported as floating-point format");
            }
        }

        internal PrimitiveValue FloatToPrimitiveValue(float value)
        {
            var fmt = (int)format;
            switch (fmt)
            {
                case kFormatBit:
                    if (sizeInBits == 1)
                        return value >= 0.5f;
                    ////FIXME: is this supposed to be int or uint?
                    return (int)NumberHelpers.NormalizedFloatToUInt(value, 0, (uint)((1UL << (int)sizeInBits) - 1));
                case kFormatSBit:
                {
                    if (sizeInBits == 1)
                        return value >= 0.0f;
                    var minValue = (int)-(long)(1UL << ((int)sizeInBits - 1));
                    var maxValue = (int)((1UL << ((int)sizeInBits - 1)) - 1);
                    return NumberHelpers.NormalizedFloatToInt(value, minValue, maxValue);
                }
                case kFormatInt:
                    Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                    return NumberHelpers.NormalizedFloatToInt(value * 0.5f + 0.5f, int.MinValue, int.MaxValue);
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                    return NumberHelpers.NormalizedFloatToUInt(value, uint.MinValue, uint.MaxValue);
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    return (short)NumberHelpers.NormalizedFloatToInt(value * 0.5f + 0.5f, short.MinValue, short.MaxValue);
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    return (ushort)NumberHelpers.NormalizedFloatToUInt(value, ushort.MinValue, ushort.MaxValue);
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    return (byte)NumberHelpers.NormalizedFloatToUInt(value, byte.MinValue, byte.MaxValue);
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    return (sbyte)NumberHelpers.NormalizedFloatToInt(value * 0.5f + 0.5f, sbyte.MinValue, sbyte.MaxValue);
                case kFormatFloat:
                    Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                    return value;
                case kFormatDouble:
                    Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                    Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                    return value;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                default:
                    throw new Exception($"State format '{format}' is not supported as floating-point format");
            }
        }

        ////REVIEW: This is some bad code duplication here between Read/WriteFloat&Double but given that there's no
        ////        way to use a type argument here, not sure how to get rid of it.

        public double ReadDouble(void* statePtr)
        {
            Debug.Assert(sizeInBits != 0);

            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                // If a control with an integer-based representation does not use the full range
                // of its integer size (e.g. only goes from [0..128]), processors or the parameters
                // above have to be used to re-process the resulting float values.
                case kFormatBit:
                    if (sizeInBits == 1)
                        return MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1.0f : 0.0f;
                    return MemoryHelpers.ReadMultipleBitsAsNormalizedUInt(valuePtr, bitOffset, sizeInBits);
                case kFormatSBit:
                    if (sizeInBits == 1)
                        return MemoryHelpers.ReadSingleBit(valuePtr, bitOffset) ? 1.0f : -1.0f;
                    return MemoryHelpers.ReadMultipleBitsAsNormalizedUInt(valuePtr, bitOffset, sizeInBits) * 2.0f - 1.0f;
                case kFormatInt:
                    Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                    return NumberHelpers.IntToNormalizedFloat(*(int*)valuePtr, int.MinValue, int.MaxValue) * 2.0f - 1.0f;
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                    return NumberHelpers.UIntToNormalizedFloat(*(uint*)valuePtr, uint.MinValue, uint.MaxValue);
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    return NumberHelpers.IntToNormalizedFloat(*(short*)valuePtr, short.MinValue, short.MaxValue) * 2.0f - 1.0f;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    return NumberHelpers.UIntToNormalizedFloat(*(ushort*)valuePtr, ushort.MinValue, ushort.MaxValue);
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    return NumberHelpers.UIntToNormalizedFloat(*valuePtr, byte.MinValue, byte.MaxValue);
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    return NumberHelpers.IntToNormalizedFloat(*(sbyte*)valuePtr, sbyte.MinValue, sbyte.MaxValue) * 2.0f - 1.0f;
                case kFormatFloat:
                    Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                    return *(float*)valuePtr;
                case kFormatDouble:
                    Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                    Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                    return *(double*)valuePtr;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                // - kFormatFloat
                // - kFormatDouble
                default:
                    throw new Exception($"State format '{format}' is not supported as floating-point format");
            }
        }

        public void WriteDouble(void* statePtr, double value)
        {
            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                case kFormatBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.5f);
                    else
                        MemoryHelpers.WriteNormalizedUIntAsMultipleBits(valuePtr, bitOffset, sizeInBits, (float)value);
                    break;
                case kFormatSBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value >= 0.0f);
                    else
                        MemoryHelpers.WriteNormalizedUIntAsMultipleBits(valuePtr, bitOffset, sizeInBits, (float)value * 0.5f + 0.5f);
                    break;
                case kFormatInt:
                    Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                    *(int*)valuePtr = NumberHelpers.NormalizedFloatToInt((float)value * 0.5f + 0.5f, int.MinValue, int.MaxValue);
                    break;
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                    *(uint*)valuePtr = NumberHelpers.NormalizedFloatToUInt((float)value, uint.MinValue, uint.MaxValue);
                    break;
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    *(short*)valuePtr = (short)NumberHelpers.NormalizedFloatToInt((float)value * 0.5f + 0.5f, short.MinValue, short.MaxValue);
                    break;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    *(ushort*)valuePtr = (ushort)NumberHelpers.NormalizedFloatToUInt((float)value, ushort.MinValue, ushort.MaxValue);
                    break;
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    *valuePtr = (byte)NumberHelpers.NormalizedFloatToUInt((float)value, byte.MinValue, byte.MaxValue);
                    break;
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    *(sbyte*)valuePtr = (sbyte)NumberHelpers.NormalizedFloatToInt((float)value * 0.5f + 0.5f, sbyte.MinValue, sbyte.MaxValue);
                    break;
                case kFormatFloat:
                    Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                    *(float*)valuePtr = (float)value;
                    break;
                case kFormatDouble:
                    Debug.Assert(sizeInBits == 64, "DBL state must have sizeInBits=64");
                    Debug.Assert(bitOffset == 0, "DBL state must be byte-aligned");
                    *(double*)valuePtr = value;
                    break;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                // - kFormatFloat
                // - kFormatDouble
                default:
                    throw new InvalidOperationException($"State format '{format}' is not supported as floating-point format");
            }
        }

        public void Write(void* statePtr, PrimitiveValue value)
        {
            var valuePtr = (byte*)statePtr + (int)byteOffset;

            var fmt = (int)format;
            switch (fmt)
            {
                case kFormatBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value.ToBoolean());
                    else
                        MemoryHelpers.WriteUIntAsMultipleBits(valuePtr, bitOffset, sizeInBits, value.ToUInt32());
                    break;
                case kFormatSBit:
                    if (sizeInBits == 1)
                        MemoryHelpers.WriteSingleBit(valuePtr, bitOffset, value.ToBoolean());
                    else
                        ////REVIEW: previous implementation was writing int32 as two's complement here
                        MemoryHelpers.WriteIntAsExcessKMultipleBits(valuePtr, bitOffset, sizeInBits, value.ToInt32());
                    break;
                case kFormatInt:
                    Debug.Assert(sizeInBits == 32, "INT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "INT state must be byte-aligned");
                    *(int*)valuePtr = value.ToInt32();
                    break;
                case kFormatUInt:
                    Debug.Assert(sizeInBits == 32, "UINT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "UINT state must be byte-aligned");
                    *(uint*)valuePtr = value.ToUInt32();
                    break;
                case kFormatShort:
                    Debug.Assert(sizeInBits == 16, "SHRT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "SHRT state must be byte-aligned");
                    *(short*)valuePtr = value.ToInt16();
                    break;
                case kFormatUShort:
                    Debug.Assert(sizeInBits == 16, "USHT state must have sizeInBits=16");
                    Debug.Assert(bitOffset == 0, "USHT state must be byte-aligned");
                    *(ushort*)valuePtr = value.ToUInt16();
                    break;
                case kFormatByte:
                    Debug.Assert(sizeInBits == 8, "BYTE state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "BYTE state must be byte-aligned");
                    *valuePtr = value.ToByte();
                    break;
                case kFormatSByte:
                    Debug.Assert(sizeInBits == 8, "SBYT state must have sizeInBits=8");
                    Debug.Assert(bitOffset == 0, "SBYT state must be byte-aligned");
                    *(sbyte*)valuePtr = value.ToSByte();
                    break;
                case kFormatFloat:
                    Debug.Assert(sizeInBits == 32, "FLT state must have sizeInBits=32");
                    Debug.Assert(bitOffset == 0, "FLT state must be byte-aligned");
                    *(float*)valuePtr = value.ToSingle();
                    break;
                // Not supported:
                // - kFormatLong
                // - kFormatULong
                // - kFormatDouble
                default:
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
