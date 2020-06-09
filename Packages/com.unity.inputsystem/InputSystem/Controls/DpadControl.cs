using System;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control made up of four discrete, directional buttons. Forms a vector
    /// but can also be addressed as individual buttons.
    /// </summary>
    /// <remarks>
    /// Is stored as four bits by default.
    ///
    /// The vector that is aggregated from the button states is normalized. I.e.
    /// even if pressing diagonally, the vector will have a length of 1 (instead
    /// of reading something like <c>(1,1)</c> for example).
    /// </remarks>
    [Scripting.Preserve]
    public class DpadControl : Vector2Control
    {
        [InputControlLayout(hideInUI = true)]
        [Scripting.Preserve]
        public class DpadAxisControl : AxisControl
        {
            public int component { get; set; }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                component = name == "x" ? 0 : 1;

                // Set the state block to be the parent's state block. We don't use that to read
                // the axis directly (we call the parent control to do that), but we need to set
                // it up the actions know to monitor this memory for changes to the control.
                m_StateBlock = m_Parent.m_StateBlock;
            }

            public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
            {
                var value = ((DpadControl)m_Parent).ReadUnprocessedValueFromState(statePtr);
                return value[component];
            }
        }

        // The DpadAxisControl has it's own logic to read state from the parent dpad.
        // The useStateFrom argument here is not actually used by that. The only reason
        // it is set up here is to avoid any state bytes being reserved for the DpadAxisControl.
        [InputControl(name = "x", layout = "DpadAxis", useStateFrom = "right", synthetic = true)]
        [InputControl(name = "y", layout = "DpadAxis", useStateFrom = "up", synthetic = true)]

        /// <summary>
        /// The button representing the vertical upwards state of the D-Pad.
        /// </summary>
        [InputControl(bit = (int)ButtonBits.Up, displayName = "Up")]
        public ButtonControl up { get; set; }

        /// <summary>
        /// The button representing the vertical downwards state of the D-Pad.
        /// </summary>
        [InputControl(bit = (int)ButtonBits.Down, displayName = "Down")]
        public ButtonControl down { get; set; }

        /// <summary>
        /// The button representing the horizontal left state of the D-Pad.
        /// </summary>
        [InputControl(bit = (int)ButtonBits.Left, displayName = "Left")]
        public ButtonControl left { get; set; }

        /// <summary>
        /// The button representing the horizontal right state of the D-Pad.
        /// </summary>
        [InputControl(bit = (int)ButtonBits.Right, displayName = "Right")]
        public ButtonControl right { get; set; }

        ////TODO: should have X and Y child controls as well

        public DpadControl()
        {
            m_StateBlock.sizeInBits = 4;
            m_StateBlock.format = InputStateBlock.FormatBit;
        }

        protected override void FinishSetup()
        {
            up = GetChildControl<ButtonControl>("up");
            down = GetChildControl<ButtonControl>("down");
            left = GetChildControl<ButtonControl>("left");
            right = GetChildControl<ButtonControl>("right");
            base.FinishSetup();
        }

        public override unsafe Vector2 ReadUnprocessedValueFromState(void* statePtr)
        {
            var upIsPressed = up.ReadValueFromState(statePtr) >= up.pressPointOrDefault;
            var downIsPressed = down.ReadValueFromState(statePtr) >= down.pressPointOrDefault;
            var leftIsPressed = left.ReadValueFromState(statePtr) >= left.pressPointOrDefault;
            var rightIsPressed = right.ReadValueFromState(statePtr) >= right.pressPointOrDefault;

            return MakeDpadVector(upIsPressed, downIsPressed, leftIsPressed, rightIsPressed);
        }

        public override unsafe void WriteValueIntoState(Vector2 value, void* statePtr)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a direction vector from the given four button states.
        /// </summary>
        /// <param name="up">Whether button representing the up direction is pressed.</param>
        /// <param name="down">Whether button representing the down direction is pressed.</param>
        /// <param name="left">Whether button representing the left direction is pressed.</param>
        /// <param name="right">Whether button representing the right direction is pressed.</param>
        /// <param name="normalize">Whether to normalize the resulting vector. If this is false, vectors in the diagonal
        /// directions will have a magnitude of greater than 1. For example, up-left will be (-1,1).</param>
        /// <returns>A 2D direction vector.</returns>
        public static Vector2 MakeDpadVector(bool up, bool down, bool left, bool right, bool normalize = true)
        {
            var upValue = up ? 1.0f : 0.0f;
            var downValue = down ? -1.0f : 0.0f;
            var leftValue = left ? -1.0f : 0.0f;
            var rightValue = right ? 1.0f : 0.0f;

            var result = new Vector2(leftValue + rightValue, upValue + downValue);

            if (normalize)
            {
                // If press is diagonal, adjust coordinates to produce vector of length 1.
                // pow(0.707107) is roughly 0.5 so sqrt(pow(0.707107)+pow(0.707107)) is ~1.
                const float diagonal = 0.707107f;
                if (result.x != 0 && result.y != 0)
                    result = new Vector2(result.x * diagonal, result.y * diagonal);
            }

            return result;
        }

        /// <summary>
        /// Create a direction vector from the given axis states.
        /// </summary>
        /// <param name="up">Axis value representing the up direction.</param>
        /// <param name="down">Axis value representing the down direction.</param>
        /// <param name="left">Axis value representing the left direction.</param>
        /// <param name="right">Axis value representing the right direction.</param>
        /// <returns>A 2D direction vector.</returns>
        public static Vector2 MakeDpadVector(float up, float down, float left, float right)
        {
            return new Vector2(-left + right, up - down);
        }

        internal enum ButtonBits
        {
            Up,
            Down,
            Left,
            Right,
        }
    }
}
