using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// Delta controls are a two-dimensional motion vector that accumulate within a frame
    /// and reset at the beginning of a frame. You can read the values from a delta control
    /// using the inherited members from Vector2Control or InputControl.
    /// </summary>
    /// <see cref="Pointer.delta"/>
    /// <seealso cref="Mouse.scroll"/>
    [Preserve]
    public class DeltaControl : Vector2Control
    {
        /// <summary>
        /// A synthetic axis representing the upper half of the Y axis value, i.e. the 0 to 1 range.
        /// </summary>
        /// <value>Control representing the control's upper half Y axis.</value>
        /// <remarks>
        /// The control is marked as <see cref="InputControl.synthetic"/>.
        /// </remarks>
        [InputControl(useStateFrom = "y", parameters = "clamp=1,clampMin=0,clampMax=3.402823E+38", synthetic = true, displayName = "Up")]
        [Preserve]
        public AxisControl up { get; set; }

        /// <summary>
        /// A synthetic axis representing the lower half of the Y axis value, i.e. the 0 to -1 range (inverted).
        /// </summary>
        /// <value>Control representing the control's lower half Y axis.</value>
        /// <remarks>
        /// The control is marked as <see cref="InputControl.synthetic"/>.
        /// </remarks>
        [InputControl(useStateFrom = "y", parameters = "clamp=1,clampMin=-3.402823E+38,clampMax=0,invert", synthetic = true, displayName = "Down")]
        [Preserve]
        public AxisControl down { get; set; }

        /// <summary>
        /// A synthetic axis representing the left half of the X axis value, i.e. the 0 to -1 range (inverted).
        /// </summary>
        /// <value>Control representing the control's left half X axis.</value>
        /// <remarks>
        /// The control is marked as <see cref="InputControl.synthetic"/>.
        /// </remarks>
        [InputControl(useStateFrom = "x", parameters = "clamp=1,clampMin=-3.402823E+38,clampMax=0,invert", synthetic = true, displayName = "Left")]
        [Preserve]
        public AxisControl left { get; set; }

        /// <summary>
        /// A synthetic axis representing the right half of the X axis value, i.e. the 0 to 1 range.
        /// </summary>
        /// <value>Control representing the control's right half X axis.</value>
        /// <remarks>
        /// The control is marked as <see cref="InputControl.synthetic"/>.
        /// </remarks>
        [InputControl(useStateFrom = "x", parameters = "clamp=1,clampMin=0,clampMax=3.402823E+38", synthetic = true, displayName = "Right")]
        [Preserve]
        public AxisControl right { get; set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            up = GetChildControl<AxisControl>("up");
            down = GetChildControl<AxisControl>("down");
            left = GetChildControl<AxisControl>("left");
            right = GetChildControl<AxisControl>("right");
        }
    }
}
