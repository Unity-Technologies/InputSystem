using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A button that is considered pressed if the underlying state has a value in the specific range.
    /// </summary>
    /// <remarks>
    /// This control is most useful for handling HID-style hat switches. Unlike <see cref="DpadControl"/>,
    /// which by default is stored as a bitfield of four bits that each represent a direction on the pad,
    /// these hat switches enumerate the possible directions that the switch can be moved in. For example,
    /// the value 1 could indicate that the switch is moved to the left whereas 3 could indicate it is
    /// moved up.
    /// </remarks>
    public class DiscreteButtonControl : ButtonControl
    {
        /// <summary>
        /// Value (inclusive) at which to start considering the button to be pressed.
        /// </summary>
        /// <remarks>
        /// <see cref="minValue"/> is allowed to be larger than <see cref="maxValue"/>. This indicates
        /// a setup where the value wraps around beyond <see cref="minValue"/>, skips <see cref="nullValue"/>,
        /// and then goes all the way up to <see cref="maxValue"/>.
        ///
        /// For example, if the underlying state represents a circular D-pad and enumerates its
        /// 9 possible positions (including null state) going clock-wise from 0 to 8 and with 1
        /// indicating that the D-pad is pressed to the left, then 1, 2, and 8 would indicate
        /// that the "left" button is held on the D-pad. To set this up, set <see cref="minValue"/>
        /// to 8, <see cref="maxValue"/> to 2, and <see cref="nullValue"/> to 0 (the default).
        /// </remarks>
        public int minValue;

        /// <summary>
        /// Value (inclusive) beyond which to stop considering the button to be pressed.
        /// </summary>
        public int maxValue;

        public int wrapAtValue;

        public int nullValue;

        public override float ReadUnprocessedValueFrom(IntPtr statePtr)
        {
            var valuePtr = new IntPtr(statePtr.ToInt64() + (int)m_StateBlock.byteOffset);
            var intValue = MemoryHelpers.ReadIntFromMultipleBits(valuePtr, m_StateBlock.bitOffset, m_StateBlock.sizeInBits);

            var value = 0.0f;
            if (minValue > maxValue)
            {
                // If no wrapping point is set, default to wrapping around exactly
                // at the point of minValue.
                if (wrapAtValue == nullValue)
                    wrapAtValue = minValue;

                if ((intValue >= minValue && intValue <= wrapAtValue)
                    || (intValue != nullValue && intValue <= maxValue))
                    value = 1.0f;
            }
            else
            {
                value = intValue >= minValue && intValue <= maxValue ? 1.0f : 0.0f;
            }

            return Preprocess(value);
        }

        protected override unsafe void WriteUnprocessedValueInto(IntPtr statePtr, float value)
        {
            throw new NotImplementedException();
        }
    }
}
