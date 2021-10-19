using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control reading a <see cref="double"/>.
    /// </summary>
    public class DoubleControl : InputControl<double>
    {
        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        public DoubleControl()
        {
            m_StateBlock.format = InputStateBlock.FormatDouble;
        }

        /// <inheritdoc/>
        public override unsafe double ReadUnprocessedValueFromState(void* statePtr)
        {
            return m_StateBlock.ReadDouble(statePtr);
        }

        /// <inheritdoc/>
        public override unsafe void WriteValueIntoState(double value, void* statePtr)
        {
            m_StateBlock.WriteDouble(statePtr, value);
        }
    }
}
