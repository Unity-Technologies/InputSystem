using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: hide in UI

namespace UnityEngine.InputSystem.Controls
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
    ///
    /// <example>
    /// <code>
    /// [StructLayout(LayoutKind.Explicit, Size = 32)]
    /// internal struct DualShock4HIDInputReport : IInputStateTypeInfo
    /// {
    ///     [FieldOffset(0)] public byte reportId;
    ///
    ///     [InputControl(name = "dpad", format = "BIT", layout = "Dpad", sizeInBits = 4, defaultState = 8)]
    ///     [InputControl(name = "dpad/up", format = "BIT", layout = "DiscreteButton", parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7", bit = 0, sizeInBits = 4)]
    ///     [InputControl(name = "dpad/right", format = "BIT", layout = "DiscreteButton", parameters = "minValue=1,maxValue=3", bit = 0, sizeInBits = 4)]
    ///     [InputControl(name = "dpad/down", format = "BIT", layout = "DiscreteButton", parameters = "minValue=3,maxValue=5", bit = 0, sizeInBits = 4)]
    ///     [InputControl(name = "dpad/left", format = "BIT", layout = "DiscreteButton", parameters = "minValue=5, maxValue=7", bit = 0, sizeInBits = 4)]
    ///     [FieldOffset(5)] public byte buttons1;
    /// }
    /// </code>
    /// </example>
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

        /// <summary>
        /// Value (inclusive) at which the values cut off and wrap back around. Considered to not be set if equal to <see cref="nullValue"/>.
        /// </summary>
        /// <remarks>
        /// This is useful if, for example, an enumeration has more bits than are used for "on" states of controls. For example,
        /// a bitfield of 4 bits where values 1-7 (i.e. 0001 to 0111) indicate "on" states of controls and value 8 indicating an
        /// "off" state. In that case, set <see cref="nullValue"/> to 8 and <see cref="wrapAtValue"/> to 7.
        /// </remarks>
        public int wrapAtValue;

        /// <summary>
        /// Value at which the button is considered to not be pressed. Usually zero. Some devices, however,
        /// use non-zero default states.
        /// </summary>
        public int nullValue;

        /// <summary>
        /// Determines the behavior of <see cref="WriteValueIntoState"/>. By default, attempting to write a <see cref="DiscreteButtonControl"/>
        /// will result in a <c>NotSupportedException</c>.
        /// </summary>
        public WriteMode writeMode;

        protected override void FinishSetup()
        {
            base.FinishSetup();

            if (!stateBlock.format.IsIntegerFormat())
                throw new NotSupportedException(
                    $"Non-integer format '{stateBlock.format}' is not supported for DiscreteButtonControl '{this}'");
        }

        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            // Note that all signed data in state buffers is in excess-K format.
            var intValue = MemoryHelpers.ReadTwosComplementMultipleBitsAsInt(valuePtr, m_StateBlock.bitOffset, m_StateBlock.sizeInBits);

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

        public override unsafe void WriteValueIntoState(float value, void* statePtr)
        {
            if (writeMode == WriteMode.WriteNullAndMaxValue)
            {
                var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
                var valueToWrite = value >= pressPointOrDefault ? maxValue : nullValue;
                MemoryHelpers.WriteIntAsTwosComplementMultipleBits(valuePtr, m_StateBlock.bitOffset, m_StateBlock.sizeInBits, valueToWrite);
                return;
            }

            // The way these controls are usually used, the state is shared between multiple DiscreteButtons. So writing one
            // may have unpredictable effects on the value of other buttons.
            throw new NotSupportedException("Writing value states for DiscreteButtonControl is not supported as a single value may correspond to multiple states");
        }

        /// <summary>
        /// How <see cref="DiscreteButtonControl.WriteValueIntoState"/> should behave.
        /// </summary>
        public enum WriteMode
        {
            /// <summary>
            /// <see cref="DiscreteButtonControl.WriteValueIntoState"/> will throw <c>NotSupportedException</c>.
            /// </summary>
            WriteDisabled = 0,

            /// <summary>
            /// Write <see cref="DiscreteButtonControl.nullValue"/> for when the button is considered not pressed and
            /// write <see cref="DiscreteButtonControl.maxValue"/> for when the button is considered pressed.
            /// </summary>
            WriteNullAndMaxValue = 1,
        }
    }
}
