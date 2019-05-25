using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Controls
{
    public class DoubleControl : InputControl<double>
    {
        public DoubleControl()
        {
            m_StateBlock.format = InputStateBlock.kTypeDouble;
        }

        public override unsafe double ReadUnprocessedValueFromState(void* statePtr)
        {
            return m_StateBlock.ReadDouble(statePtr);
        }

        public override unsafe void WriteValueIntoState(double value, void* statePtr)
        {
            m_StateBlock.WriteDouble(statePtr, value);
        }
    }
}
