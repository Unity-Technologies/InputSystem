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

        // Type identifier for the memory layout used by the state. Used for safety checks to
        // make sure that when we do memory copies of entire state blocks, we copy between
        // identical layouts.
        public FourCC typeCode;

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


        [Flags]
        private enum Flags
        {
            Allocated = 1 << 0, // Whether we have allocated space for this block.
            SemanticsOutput = 1 << 1,
        }

        private Flags m_Flags;

        // These fields are owned and managed by InputManager.
        internal static IntPtr s_CurrentStatePtr;
        internal static IntPtr s_PreviousStatePtr;

        internal IntPtr currentStatePtr
        {
            get { return s_CurrentStatePtr + (int)byteOffset; }
        }

        internal IntPtr previousStatePtr
        {
            get { return s_PreviousStatePtr + (int)byteOffset; }
        }

        internal bool isAllocated
        {
            get { return (m_Flags & Flags.Allocated) == Flags.Allocated; }
            set
            {
                if (value)
                    m_Flags |= Flags.Allocated;
                else
                    m_Flags &= ~Flags.Allocated;
            }
        }

        internal int alignedSizeInBytes
        {
            get { return (int)(sizeInBits / 8) + (sizeInBits % 8 > 0 ? 1 : 0); }
        }
    }
}
