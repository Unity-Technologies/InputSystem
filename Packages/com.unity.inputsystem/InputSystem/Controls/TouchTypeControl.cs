using System;
using UnityEngine.Experimental.Input.LowLevel;


namespace UnityEngine.Experimental.Input.Controls
{
    public class TouchTypeControl : InputControl<TouchType>
    {
        public TouchTypeControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeSByte;
        }

        public override unsafe TouchType ReadRawValueFrom(IntPtr statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (TouchType)intValue;
        }

        protected override unsafe void WriteRawValueInto(IntPtr statePtr, TouchType value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            *(TouchType*)valuePtr = value;
        }
    }
}
