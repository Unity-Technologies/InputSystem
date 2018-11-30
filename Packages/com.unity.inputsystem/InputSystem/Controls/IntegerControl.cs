using UnityEngine.Experimental.Input.LowLevel;

////TODO: this or the layout system needs to detect when the format isn't supported by the control

namespace UnityEngine.Experimental.Input.Controls
{
    ////TODO: allow format to be any integer format
    public class IntegerControl : InputControl<int>
    {
        public IntegerControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeInt;
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
