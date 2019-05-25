using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;

// Unfortunately, C# (at least up to version 6) does not support enum type constraints. There's
// ways to work around it in some situations (https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum)
// but not in a way that will allow us to convert an int to the enum type.

namespace UnityEngine.Experimental.Input.Controls
{
    [InputControlLayout(hideInUI = true)]
    internal class TouchPhaseControl : InputControl<TouchPhase>
    {
        public TouchPhaseControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeInt;
        }

        public override unsafe TouchPhase ReadUnprocessedValueFromState(void* statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (TouchPhase)intValue;
        }

        public override unsafe void WriteValueIntoState(TouchPhase value, void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(TouchPhase*)valuePtr = value;
        }
    }
}
