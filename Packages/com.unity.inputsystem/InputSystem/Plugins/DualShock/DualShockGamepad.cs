using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

////TODO: speaker, touchpad

namespace UnityEngine.InputSystem.Plugins.DualShock
{
    /// <summary>
    /// A Sony DualShock controller.
    /// </summary>
    [InputControlLayout(displayName = "PS4 Controller")]
    public class DualShockGamepad : Gamepad, IDualShockHaptics
    {
        public ButtonControl touchpadButton { get; private set; }

        [InputControl(name = "start", displayName = "Options")]
        public ButtonControl optionsButton { get; private set; }

        [InputControl(name = "select", displayName = "Share")]
        public ButtonControl shareButton { get; private set; }

        [InputControl(name = "buttonWest", displayName = "Square", shortDisplayName = "\u25A1")]
        public ButtonControl squareButton { get; private set; }

        [InputControl(name = "buttonNorth", displayName = "Triangle", shortDisplayName = "\u25B3")]
        public ButtonControl triangleButton { get; private set; }

        [InputControl(name = "buttonEast", displayName = "Circle", shortDisplayName = "\u25CB")]
        public ButtonControl circleButton { get; private set; }

        [InputControl(name = "buttonSouth", displayName = "Cross", shortDisplayName = "\u274C")]
        public ButtonControl crossButton { get; private set; }

        [InputControl(name = "leftShoulder", shortDisplayName = "L1")]
        public ButtonControl L1 { get; private set; }

        [InputControl(name = "rightShoulder", shortDisplayName = "R1")]
        public ButtonControl R1 { get; private set; }

        [InputControl(name = "leftTrigger", shortDisplayName = "L2")]
        public ButtonControl L2 { get; private set; }

        [InputControl(name = "rightTrigger", shortDisplayName = "R2")]
        public ButtonControl R2 { get; private set; }

        [InputControl(name = "leftStickPress", shortDisplayName = "L3")]
        public ButtonControl L3 { get; private set; }

        [InputControl(name = "rightStickPress", shortDisplayName = "R3")]
        public ButtonControl R3 { get; private set; }

        public new static DualShockGamepad current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            touchpadButton = builder.GetControl<ButtonControl>(this, "touchpadButton");
            optionsButton = startButton;
            shareButton = selectButton;

            squareButton = buttonWest;
            triangleButton = buttonNorth;
            circleButton = buttonEast;
            crossButton = buttonSouth;

            L1 = leftShoulder;
            R1 = rightShoulder;
            L2 = leftTrigger;
            R2 = rightTrigger;
            L3 = leftStickButton;
            R3 = rightStickButton;
        }

        public virtual void SetLightBarColor(Color color)
        {
        }
    }
}
