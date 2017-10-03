using System;

namespace ISX
{
    ////REVIEW: shouldn't this still have a float value type?
    public class ButtonControl : InputControl<bool>
    {
        public ButtonControl()
        {
            m_StateBlock.sizeInBits = 1;
        }

        public override bool value
        {
            get { return GetValue(currentStatePtr); }
        }

        public bool wasPressedThisFrame
        {
            get { return value != GetValue(previousStatePtr); }
        }

        protected unsafe bool GetValue(IntPtr statePtr)
        {
            var buffer = (byte*)statePtr;
            return Process((buffer[m_StateBlock.byteOffset] & (1 << (int)m_StateBlock.bitOffset)) == (byte)(1 << (int)m_StateBlock.bitOffset));
        }
    }
}
