using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

////TODO: set displayNames of the controls according to PlayStation controller standards

////TODO: speaker, touchpad

namespace UnityEngine.Experimental.Input.Plugins.DualShock
{
    /// <summary>
    /// A Sony DualShock controller.
    /// </summary>
    [InputControlLayout] // Unset state type inherited from base.
    public class DualShockGamepad : Gamepad, IDualShockHaptics
    {
        public ButtonControl touchpadButton { get; private set; }
        public ButtonControl optionsButton { get; private set; }

        public ButtonControl squareButton { get; private set; }
        public ButtonControl triangleButton { get; private set; }
        public ButtonControl circleButton { get; private set; }
        public ButtonControl crossButton { get; private set; }

        public Vector3Control acceleration { get; private set; }
        public QuaternionControl orientation { get; private set; }
        public Vector3Control angularVelocity { get; private set; }

        public new static DualShockGamepad current { get; private set; }

        public ButtonControl L3 { get; private set; }
        public ButtonControl R3 { get; private set; }
        public ButtonControl L2 { get; private set; }
        public ButtonControl R2 { get; private set; }
        public ButtonControl L1 { get; private set; }
        public ButtonControl R1 { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            touchpadButton = builder.GetControl<ButtonControl>(this, "touchpadButton");
            optionsButton = startButton;

            acceleration = builder.GetControl<Vector3Control>(this, "acceleration");
            orientation = builder.GetControl<QuaternionControl>(this, "orientation");
            angularVelocity = builder.GetControl<Vector3Control>(this, "angularVelocity");

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

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        public virtual void SetLightBarColor(Color color)
        {
        }
    }
}
