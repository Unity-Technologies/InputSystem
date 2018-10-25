using System;
using UnityEngine.Experimental.Input.LowLevel;

// Unfortunately, C# (at least up to version 6) does not support enum type constraints. There's
// ways to work around it in some situations (https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum)
// but not in a way that will allow us to convert an int to the enum type.

////TODO: allow this to be stored in less than 32bits

namespace UnityEngine.Experimental.Input.Controls
{
    public class PointerPhaseControl : InputControl<PointerPhase>
    {
        public PointerPhaseControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeInt;
        }

        public override unsafe PointerPhase ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (PointerPhase)intValue;
        }

        protected override unsafe void WriteUnprocessedValueInto(IntPtr statePtr, PointerPhase value)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            *(PointerPhase*)valuePtr = value;
        }
    }
}
