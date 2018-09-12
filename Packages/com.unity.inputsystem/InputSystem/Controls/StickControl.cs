using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Processors;

namespace UnityEngine.Experimental.Input.Controls
{
    /// <summary>
    /// A two-axis thumbstick control that can act as both a vector and a four-way dpad.
    /// </summary>
    /// <remarks>
    /// State-wise this is still just a Vector2.
    ///
    /// Unlike <see cref="DpadControl">D-Pads</see>, sticks will usually have <see cref="DeadzoneProcessor">
    /// deadzone processors</see> applied to them.
    /// </remarks>
    public class StickControl : Vector2Control
    {
        [InputControl(useStateFrom = "y", parameters = "clamp,clampMin=0,clampMax=1")]
        public ButtonControl up { get; private set; }
        [InputControl(useStateFrom = "y", parameters = "clamp,clampMin=-1,clampMax=0,invert")]
        public ButtonControl down { get; private set; }
        [InputControl(useStateFrom = "x", parameters = "clamp,clampMin=-1,clampMax=0,invert")]
        public ButtonControl left { get; private set; }
        [InputControl(useStateFrom = "x", parameters = "clamp,clampMin=0,clampMax=1")]
        public ButtonControl right { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            up = builder.GetControl<ButtonControl>(this, "up");
            down = builder.GetControl<ButtonControl>(this, "down");
            left = builder.GetControl<ButtonControl>(this, "left");
            right = builder.GetControl<ButtonControl>(this, "right");

            base.FinishSetup(builder);
        }
    }
}
