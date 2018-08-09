using System;
using UnityEngine.Experimental.Input.LowLevel;


namespace UnityEngine.Experimental.Input.Controls
{
    public class TouchFlagsControl : InputControl<TouchFlags>
    {
        public TouchFlagsControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeSByte;
        }

        public override unsafe TouchFlags ReadRawValueFrom(IntPtr statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (TouchFlags)intValue;
        }

        protected override unsafe void WriteRawValueInto(IntPtr statePtr, TouchFlags value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            *(TouchFlags*)valuePtr = value;
        }
    }
}
