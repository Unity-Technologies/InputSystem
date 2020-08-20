using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control reading a <see cref="ScreenKeyboardStatus"/> value.
    /// </summary>
    /// <remarks>
    /// This is used mainly by <see cref="ScreenKeyboard"/> to read <see cref="ScreenKeyboard.status"/>.
    /// </remarks>
    /// <seealso cref="ScreenKeyboard"/>
    [InputControlLayout(hideInUI = true)]
    [Scripting.Preserve]
    public class ScreenKeyboardStatusControl : InputControl<ScreenKeyboardStatus>
    {
        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        /// <remarks>
        /// Format of the control is <see cref="InputStateBlock.FormatInt"/>
        /// by default.
        /// </remarks>
        public ScreenKeyboardStatusControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
        }

        /// <inheritdoc />
        public override unsafe ScreenKeyboardStatus ReadUnprocessedValueFromState(void* statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (ScreenKeyboardStatus)intValue;
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(ScreenKeyboardStatus value, void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(ScreenKeyboardStatus*)valuePtr = value;
        }
    }
}
