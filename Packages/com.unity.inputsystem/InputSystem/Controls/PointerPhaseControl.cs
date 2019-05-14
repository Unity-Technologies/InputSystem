using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

// Unfortunately, C# (at least up to version 6) does not support enum type constraints. There's
// ways to work around it in some situations (https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum)
// but not in a way that will allow us to convert an int to the enum type.

////TODO: allow this to be stored in less than 32bits

namespace UnityEngine.InputSystem.Controls
{
    [InputControlLayout(hideInUI = true)]
    public class PointerPhaseControl : InputControl<PointerPhase>
    {
        public PointerPhaseControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
        }

        public override unsafe PointerPhase ReadUnprocessedValueFromState(void* statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (PointerPhase)intValue;
        }

        public override unsafe void WriteValueIntoState(PointerPhase value, void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(PointerPhase*)valuePtr = value;
        }
    }
}
