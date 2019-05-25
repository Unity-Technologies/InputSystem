using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Controls
{
    public class IntegerControl : InputControl<int>
    {
        public IntegerControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeInt;
        }

        public override unsafe int ReadUnprocessedValueFromState(void* statePtr)
        {
            return m_StateBlock.ReadInt(statePtr);
        }

        public override unsafe void WriteValueIntoState(int value, void* statePtr)
        {
            m_StateBlock.WriteInt(statePtr, value);
        }
    }
}
