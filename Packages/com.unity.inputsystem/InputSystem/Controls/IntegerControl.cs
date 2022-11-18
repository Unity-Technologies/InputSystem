using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

////TODO: this or the layout system needs to detect when the format isn't supported by the control

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A generic input control reading integer values.
    /// </summary>
    public class IntegerControl : InputControl<int>
    {
        /// <summary>
        /// Default-initialize an integer control.
        /// </summary>
        public IntegerControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
        }

        /// <inheritdoc/>
        public override unsafe int ReadUnprocessedValueFromState(void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatInt:
                    return *(int*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
                default:
                    return m_StateBlock.ReadInt(statePtr);
            }
        }

        /// <inheritdoc/>
        public override unsafe void WriteValueIntoState(int value, void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatInt:
                    *(int*)((byte*)statePtr + (int)m_StateBlock.byteOffset) = value;
                    break;
                default:
                    m_StateBlock.WriteInt(statePtr, value);
                    break;
            }
        }

        protected override FourCC CalculateOptimizedControlDataType()
        {
            if (m_StateBlock.format == InputStateBlock.FormatInt &&
                m_StateBlock.sizeInBits == 32 &&
                m_StateBlock.bitOffset == 0)
                return InputStateBlock.FormatInt;
            return InputStateBlock.FormatInvalid;
        }
    }
}
