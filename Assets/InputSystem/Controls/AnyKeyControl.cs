using System;

namespace ISX
{
    // A control that simply checks the entire state it's been assigned
    // for whether there's any non-zero bytes. If there are, the control
    // returns 1.0; otherwise it returns 0.0.
    public class AnyKeyControl : InputControl<float>
    {
        public bool isPressed => value > 0.0f;

        public AnyKeyControl()
        {
            m_StateBlock.sizeInBits = 1; // Should be overridden by whoever uses the control.
            m_StateBlock.format = InputStateBlock.kTypeBit;
        }

        protected override float ReadRawValueFrom(IntPtr statePtr)
        {
            var valuePtr = statePtr + (int)m_StateBlock.byteOffset;
            return CheckStateIsAllZeros(valuePtr) ? 0.0f : 1.0f;
        }
    }
}
