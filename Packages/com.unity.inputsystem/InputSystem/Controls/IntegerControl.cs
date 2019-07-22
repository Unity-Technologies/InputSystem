using UnityEngine.InputSystem.LowLevel;

////TODO: this or the layout system needs to detect when the format isn't supported by the control

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A generic input control reading integer values.
    /// </summary>
    public class IntegerControl : InputControl<int>
    {
        public IntegerControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
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
