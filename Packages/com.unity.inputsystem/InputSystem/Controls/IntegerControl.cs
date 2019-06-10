using UnityEngine.InputSystem.LowLevel;

////TODO: this or the layout system needs to detect when the format isn't supported by the control

namespace UnityEngine.InputSystem.Controls
{
    ////TODO: allow format to be any integer format
    public class IntegerControl : InputControl<int>
    {
        public IntegerControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
        }

        public override unsafe int ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            return *(int*)valuePtr;
        }

        public override unsafe void WriteValueIntoState(int value, void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(int*)valuePtr = value;
        }
    }
}
