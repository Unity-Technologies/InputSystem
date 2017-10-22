using System;

namespace ISX
{
    // A control that simply checks the entire state it's been assigned
    // for whether there's any non-zero bytes. If there are, the control
    // returns 1.0; otherwise it returns 0.0.
    public class AnyKeyControl : InputControl<float>
    {
        public override float value => Process(GetValueFrom(currentValuePtr));
        public override float previous => Process(GetValueFrom(previousValuePtr));

        public bool isPressed => value > 0.0f;

        public AnyKeyControl()
        {
            m_StateBlock.sizeInBits = 1; // Should be overridden by whoever uses the control.
            m_StateBlock.format = InputStateBlock.kTypeBit;
        }

        protected float GetValueFrom(IntPtr valuePtr)
        {
            return CheckStateIsAllZeros(valuePtr) ? 0.0f : 1.0f;
        }
    }
}
