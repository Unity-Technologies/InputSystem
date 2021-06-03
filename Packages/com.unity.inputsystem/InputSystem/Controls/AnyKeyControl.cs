using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

////REVIEW: generalize this to AnyButton and add to more devices?

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control that simply checks the entire state it's been assigned
    /// for whether there's any non-zero bytes. If there are, the control
    /// returns 1.0; otherwise it returns 0.0.
    /// </summary>
    /// <remarks>
    /// This control is used by <see cref="Keyboard.anyKey"/> to create a button
    /// that is toggled on as long as any of the keys on the keyboard is pressed.
    /// </remarks>
    /// <seealso cref="Keyboard.anyKey"/>
    [InputControlLayout(hideInUI = true)]
    [Scripting.Preserve]
    public class AnyKeyControl : ButtonControl
    {
        ////TODO: wasPressedThisFrame and wasReleasedThisFrame

        /// <summary>
        /// Default initialization. Sets state size to 1 bit and format to
        /// <see cref="InputStateBlock.FormatBit"/>.
        /// </summary>
        public AnyKeyControl()
        {
            m_StateBlock.sizeInBits = 1; // Should be overridden by whoever uses the control.
            m_StateBlock.format = InputStateBlock.FormatBit;
        }

        /// <inheritdoc />
        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            return this.CheckStateIsAtDefault(statePtr) ? 0.0f : 1.0f;
        }
    }
}
