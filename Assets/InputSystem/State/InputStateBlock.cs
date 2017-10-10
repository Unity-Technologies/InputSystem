using System;

namespace ISX
{
    // Input state is kept in raw memory blocks.
    // All state is centrally managed by InputManager; controls cannot keep their own independent state.
    // State can be used to store values received from external systems (input) or to accumulate values to
    // send back to external systems (output).
    // NOTE: Generally, there is no need to futz around with state directly; stick to InputControls
    //       to do the heavy-lifting.
    public struct InputStateBlock
    {
        public enum Semantics
        {
            Input, // State captures values coming in.
            Output // State captures values going out.
        }

        public const uint kInvalidOffset = 0xffffffff;

        // Primitive state type codes.
        public static FourCC kTypeBit = new FourCC('B', 'I', 'T');
        public static FourCC kTypeInt = new FourCC('I', 'N', 'T');
        public static FourCC kTypeShort = new FourCC('S', 'H', 'R', 'T');
        public static FourCC kTypeByte = new FourCC('B', 'Y', 'T', 'E');
        public static FourCC kTypeFloat = new FourCC('F', 'L', 'T');
        public static FourCC kTypeDouble = new FourCC('D', 'B', 'L');
        public static FourCC kTypeVector2 = new FourCC('V', 'E', 'C', '2');
        public static FourCC kTypeVector3 = new FourCC('V', 'E', 'C', '2');
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

        // Type identifier for the memory layout used by the state. Used for safety checks to
        // make sure that when we do memory copies of entire state blocks, we copy between
        // identical layouts.
        public FourCC format;

        public uint byteOffset;
        public uint bitOffset;
        public uint sizeInBits;

        public Semantics semantics
        {
            get
            {
                if ((m_Flags & Flags.SemanticsOutput) == Flags.SemanticsOutput)
                    return Semantics.Input;
                return Semantics.Input;
            }
            set
            {
                if (value == Semantics.Input)
                    m_Flags &= ~Flags.SemanticsOutput;
                else
                    m_Flags |= Flags.SemanticsOutput;
            }
        }

        public bool isBitfield => sizeInBits % 8 != 0;


        [Flags]
        private enum Flags
        {
            SemanticsOutput = 1 << 0,
        }

        private Flags m_Flags;

        internal int alignedSizeInBytes => (int)(sizeInBits / 8) + (sizeInBits % 8 > 0 ? 1 : 0);
    }
}
