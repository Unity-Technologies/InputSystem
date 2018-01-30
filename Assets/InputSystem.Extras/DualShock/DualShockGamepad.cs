using UnityEngine;

////TODO: set displayNames of the controls according to PlayStation controller standards

////TODO: gyro, speaker, touchpad

namespace ISX.DualShock
{
    /// <summary>
    /// A PS4 DualShock controller.
    /// </summary>
    public class DualShockGamepad : Gamepad, IDualShockHaptics
    {
        public ButtonControl touchpadButton { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            touchpadButton = setup.GetControl<ButtonControl>(this, "touchpadButton");

            base.FinishSetup(setup);
        }

        public virtual void SetLightBarColor(Color color)
        {
            throw new System.NotImplementedException();
        }
    }
}
