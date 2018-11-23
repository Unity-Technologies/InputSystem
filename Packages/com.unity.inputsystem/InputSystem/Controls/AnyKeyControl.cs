using System;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A control that simply checks the entire state it's been assigned
    /// for whether there's any non-zero bytes. If there are, the control
    /// returns 1.0; otherwise it returns 0.0.
    /// </summary>
    public class AnyKeyControl : InputControl<float>////TODO: this should be a ButtonControl
    {
        public bool isPressed
        {
            get { return ReadValue() > 0.0f; }
        }

        ////TODO: wasPressedThisFrame and wasReleasedThisFrame

        public AnyKeyControl()
        {
            m_StateBlock.sizeInBits = 1; // Should be overridden by whoever uses the control.
            m_StateBlock.format = InputStateBlock.kTypeBit;
        }

        public override unsafe float ReadUnprocessedValueFrom(void* statePtr)
        {
            return CheckStateIsAtDefault(statePtr) ? 0.0f : 1.0f;
        }
    }
}
