using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

// Unfortunately, C# (at least up to version 6) does not support enum type constraints. There's
// ways to work around it in some situations (https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum)
// but not in a way that will allow us to convert an int to the enum type.

////TODO: allow this to be stored in less than 32bits

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control reading a <see cref="TouchPhase"/> value.
    /// </summary>
    /// <remarks>
    /// This is used mainly by <see cref="Touchscreen"/> to read <see cref="TouchState.phase"/>.
    /// </remarks>
    /// <seealso cref="Touchscreen"/>
    [InputControlLayout(hideInUI = true)]
    [Scripting.Preserve]
    public class TouchPhaseControl : InputControl<TouchPhase>
    {
        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        /// <remarks>
        /// Format of the control is <see cref="InputStateBlock.FormatInt"/>
        /// by default.
        /// </remarks>
        public TouchPhaseControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
        }

        /// <inheritdoc />
        public override unsafe TouchPhase ReadUnprocessedValueFromState(void* statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (TouchPhase)intValue;
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(TouchPhase value, void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(TouchPhase*)valuePtr = value;
        }
    }
}
