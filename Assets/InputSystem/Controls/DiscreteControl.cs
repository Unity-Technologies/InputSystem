using System;
using ISX.LowLevel;

namespace ISX
{
    ////TODO: allow format to be any integer format
    public class DiscreteControl : InputControl<int>
    {
        public DiscreteControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeInt;
        }

        protected override unsafe int ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            return *(int*)valuePtr;
        }

        protected override unsafe void WriteRawValueInto(IntPtr statePtr, int value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            *(int*)valuePtr = value;
        }
    }
}
