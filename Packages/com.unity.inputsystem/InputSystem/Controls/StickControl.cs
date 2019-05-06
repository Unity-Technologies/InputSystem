using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A two-axis thumbstick control that can act as both a vector and a four-way dpad.
    /// </summary>
    /// <remarks>
    /// State-wise this is still just a Vector2.
    ///
    /// Unlike <see cref="DpadControl">D-Pads</see>, sticks will usually have <see cref="StickDeadzoneProcessor">
    /// deadzone processors</see> applied to them.
    /// </remarks>
    public class StickControl : Vector2Control
    {
        ////REVIEW: should X and Y have "Horizontal" and "Vertical" as long display names and "X" and "Y" as short names?
        // Set min&max on XY axes.
        // Also put AxisDeadzones on the axes.
        [InputControl(name = "x", minValue = -1f, maxValue = 1f, layout = "Axis", processors = "axisDeadzone")]
        [InputControl(name = "y", minValue = -1f, maxValue = 1f, layout = "Axis", processors = "axisDeadzone")]

        // Buttons for each of the directions. Allows the stick to function as a dpad.
        // Note that these controls are marked as synthetic as there isn't real buttons for the half-axes
        // on the device. This aids in interactive picking by making sure that if we have to decide between,
        // say, leftStick/x and leftStick/left, leftStick/x wins out.

        ////REVIEW: up/down/left/right should probably prohibit being written to

        /// <summary>
        /// A synthetic button representing the upper half of the stick's Y axis.
        /// </summary>
        [InputControl(useStateFrom = "y", parameters = "clamp,clampMin=0,clampMax=1", synthetic = true, displayName = "Up", shortDisplayName = "\u2191")]
        public ButtonControl up { get; private set; }

        [InputControl(useStateFrom = "y", parameters = "clamp,clampMin=-1,clampMax=0,invert", synthetic = true, displayName = "Down", shortDisplayName = "\u2193")]
        public ButtonControl down { get; private set; }

        [InputControl(useStateFrom = "x", parameters = "clamp,clampMin=-1,clampMax=0,invert", synthetic = true, displayName = "Left", shortDisplayName = "\u2190")]
        public ButtonControl left { get; private set; }

        [InputControl(useStateFrom = "x", parameters = "clamp,clampMin=0,clampMax=1", synthetic = true, displayName = "Right", shortDisplayName = "\u2192")]
        public ButtonControl right { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            up = builder.GetControl<ButtonControl>(this, "up");
            down = builder.GetControl<ButtonControl>(this, "down");
            left = builder.GetControl<ButtonControl>(this, "left");
            right = builder.GetControl<ButtonControl>(this, "right");
        }
    }
}
