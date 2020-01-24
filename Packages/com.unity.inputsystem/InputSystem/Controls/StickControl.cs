using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A two-axis thumbstick control that can act as both a vector and a four-way dpad.
    /// </summary>
    /// <remarks>
    /// Stick controls are used to represent the thumbsticks on gamepads (see <see cref="Gamepad.leftStick"/>
    /// and <see cref="Gamepad.rightStick"/>) as well as the main stick control of joysticks (see
    /// <see cref="Joystick.stick"/>).
    ///
    /// Essentially, a stick is an extended <c>Vector2</c> control that can function either
    /// as a combined 2D vector, as independent vertical and horizontal axes, or as four
    /// individual, directional buttons. The following example demonstrates this based on the
    /// gamepad's left stick.
    ///
    /// <example>
    /// <code>
    /// // Read stick as a combined 2D vector.
    /// Gamepad.current.leftStick.ReadValue();
    ///
    /// // Read X and Y axis of stick individually.
    /// Gamepad.current.leftStick.x.ReadValue();
    /// Gamepad.current.leftStick.y.ReadValue();
    ///
    /// // Read the stick as four individual directional buttons.
    /// Gamepad.current.leftStick.up.ReadValue();
    /// Gamepad.current.leftStick.down.ReadValue();
    /// Gamepad.current.leftStick.left.ReadValue();
    /// Gamepad.current.leftStick.right.ReadValue();
    /// </code>
    /// </example>
    ///
    /// In terms of memory, a stick controls is still just from one value for the X axis
    /// and one value for the Y axis.
    ///
    /// Unlike dpads (see <see cref="DpadControl"/>), sticks will usually have deadzone processors
    /// (see <see cref="StickDeadzoneProcessor"/>) applied to them to get rid of noise around the
    /// resting point of the stick. The X and Y axis also have deadzones applied to them by
    /// default (<see cref="AxisDeadzoneProcessor"/>). Note, however, that the deadzoning of
    /// individual axes is different from the deadzoning applied to the stick as a whole and
    /// thus does not have to result in exactly the same values. Deadzoning of individual axes
    /// is linear (i.e. the result is simply clamped and normalized back into [0..1] range) whereas
    /// the deadzoning of sticks is radial (i.e. the length of the vector is taken into account
    /// which means that <em>both</em> the X and Y axis contribute).
    /// </remarks>
    [Scripting.Preserve]
    public class StickControl : Vector2Control
    {
        ////REVIEW: should X and Y have "Horizontal" and "Vertical" as long display names and "X" and "Y" as short names?

        // Buttons for each of the directions. Allows the stick to function as a dpad.
        // Note that these controls are marked as synthetic as there isn't real buttons for the half-axes
        // on the device. This aids in interactive picking by making sure that if we have to decide between,
        // say, leftStick/x and leftStick/left, leftStick/x wins out.

        ////REVIEW: up/down/left/right should probably prohibit being written to
        ////REVIEW: Should up/down/left/control actually be their own control types that *read* the values
        ////        from X and Y instead of sharing their state? The current setup easily leads to various
        ////        problems because more than just the state block is needed to read the value of a control
        ////        from state correctly.

        /// <summary>
        /// A synthetic button representing the upper half of the stick's Y axis.
        /// </summary>
        /// <value>Control representing the stick's upper half Y axis.</value>
        /// <remarks>
        ///
        /// The control is marked as <see cref="InputControl.synthetic"/>.
        /// </remarks>
        [InputControl(useStateFrom = "y", processors = "axisDeadzone", parameters = "clamp=2,clampMin=0,clampMax=1", synthetic = true, displayName = "Up")]
        // Set min&max on XY axes. We do this here as the documentation generator will not be happy
        // if we place this above the doc comment.
        // Also puts AxisDeadzones on the axes.
        [InputControl(name = "x", minValue = -1f, maxValue = 1f, layout = "Axis", processors = "axisDeadzone")]
        [InputControl(name = "y", minValue = -1f, maxValue = 1f, layout = "Axis", processors = "axisDeadzone")]
        [Scripting.Preserve]
        public ButtonControl up { get; private set; }

        [InputControl(useStateFrom = "y", processors = "axisDeadzone", parameters = "clamp=2,clampMin=-1,clampMax=0,invert", synthetic = true, displayName = "Down")]
        [Scripting.Preserve]
        public ButtonControl down { get; private set; }

        [InputControl(useStateFrom = "x", processors = "axisDeadzone", parameters = "clamp=2,clampMin=-1,clampMax=0,invert", synthetic = true, displayName = "Left")]
        [Scripting.Preserve]
        public ButtonControl left { get; private set; }

        [InputControl(useStateFrom = "x", processors = "axisDeadzone", parameters = "clamp=2,clampMin=0,clampMax=1", synthetic = true, displayName = "Right")]
        [Scripting.Preserve]
        public ButtonControl right { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            up = GetChildControl<ButtonControl>("up");
            down = GetChildControl<ButtonControl>("down");
            left = GetChildControl<ButtonControl>("left");
            right = GetChildControl<ButtonControl>("right");
        }
    }
}
