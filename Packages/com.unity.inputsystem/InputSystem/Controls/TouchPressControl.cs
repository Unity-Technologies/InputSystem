using System;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A button that reads its pressed state from <see cref="TouchControl.phase"/>.
    /// </summary>
    [InputControlLayout(hideInUI = true)]
    internal class TouchPressControl : ButtonControl
    {
        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            if (!stateBlock.format.IsIntegerFormat())
                throw new NotSupportedException(
                    $"Non-integer format '{stateBlock.format}' is not supported for TouchButtonControl '{this}'");
        }

        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            var intValue = MemoryHelpers.ReadIntFromMultipleBits(valuePtr, m_StateBlock.bitOffset, m_StateBlock.sizeInBits);
            var phaseValue = (TouchPhase)intValue;

            var value = 0.0f;
            if (phaseValue == TouchPhase.Began || phaseValue == TouchPhase.Stationary ||
                phaseValue == TouchPhase.Moved)
                value = 1;

            return Preprocess(value);
        }
    }
}
