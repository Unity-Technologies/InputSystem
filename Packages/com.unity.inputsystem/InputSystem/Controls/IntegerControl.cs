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

        public override unsafe int ReadUnprocessedValueFrom(void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            return *(int*)valuePtr;
        }

        protected override unsafe void WriteUnprocessedValueInto(void* statePtr, int value)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(int*)valuePtr = value;
        }
    }
}
