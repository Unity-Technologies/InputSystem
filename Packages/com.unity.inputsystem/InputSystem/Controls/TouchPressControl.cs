using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A button that reads its pressed state from <see cref="TouchControl.phase"/>.
    /// </summary>
    /// <remarks>
    /// This control is used by <see cref="TouchControl"/> to link <see cref="TouchControl.press"/>
    /// to <see cref="TouchControl.phase"/>. It will return 1 as long as the value of
    /// phase is <see cref="TouchPhase.Began"/>, <see cref="TouchPhase.Stationary"/>, or
    /// <see cref="TouchPhase.Moved"/>, i.e. as long as the touch is in progress. For
    /// all other phases, it will return 0.
    /// </remarks>
    /// <seealso cref="TouchControl"/>
    [InputControlLayout(hideInUI = true)]
    [Scripting.Preserve]
    public class TouchPressControl : ButtonControl
    {
        /// <inheritdoc />
        protected override void FinishSetup()
        {
            base.FinishSetup();

            if (!stateBlock.format.IsIntegerFormat())
                throw new NotSupportedException(
                    $"Non-integer format '{stateBlock.format}' is not supported for TouchButtonControl '{this}'");
        }

        /// <inheritdoc />
        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            var uintValue = MemoryHelpers.ReadMultipleBitsAsUInt(valuePtr, m_StateBlock.bitOffset, m_StateBlock.sizeInBits);
            var phaseValue = (TouchPhase)uintValue;

            var value = 0.0f;
            if (phaseValue == TouchPhase.Began || phaseValue == TouchPhase.Stationary ||
                phaseValue == TouchPhase.Moved)
                value = 1;

            return Preprocess(value);
        }

        public override unsafe void WriteValueIntoState(float value, void* statePtr)
        {
            throw new NotSupportedException();
        }
    }
}
