using ISX.Controls;
using UnityEngine;

////TODO: set displayNames of the controls according to PlayStation controller standards

////TODO: speaker, touchpad

namespace ISX.Plugins.DualShock
{
    /// <summary>
    /// A PS4 DualShock controller.
    /// </summary>
    public abstract class DualShockGamepad : Gamepad, IDualShockHaptics
    {
        public ButtonControl touchpadButton { get; private set; }

        public Vector3Control acceleration { get; private set; }
        public Vector3Control gyro { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            touchpadButton = setup.GetControl<ButtonControl>(this, "touchpadButton");
            acceleration = setup.GetControl<Vector3Control>(this, "acceleration");
            gyro = setup.GetControl<Vector3Control>(this, "gyro");

            base.FinishSetup(setup);
        }

        public abstract void SetLightBarColor(Color color);
    }
}
