using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control that simply checks the entire state it's been assigned
    /// for whether there's any non-zero bytes. If there are, the control
    /// returns 1.0; otherwise it returns 0.0.
    /// </summary>
    public class AnyKeyControl : InputControl<float>////TODO: this should be a ButtonControl
    {
        public bool isPressed => ReadValue() > 0.0f;

        ////TODO: wasPressedThisFrame and wasReleasedThisFrame

        public AnyKeyControl()
        {
            m_StateBlock.sizeInBits = 1; // Should be overridden by whoever uses the control.
            m_StateBlock.format = InputStateBlock.FormatBit;
        }

        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            return this.CheckStateIsAtDefault(statePtr) ? 0.0f : 1.0f;
        }
    }
}
