using UnityEngine.InputSystem.Haptics;

namespace UnityEngine.InputSystem.DualShock
{
    /// <summary>
    /// Extended haptics interface for DualShock controllers.
    /// </summary>
    public interface IDualShockHaptics : IDualMotorRumble
    {
        /// <summary>
        /// Set the color of the light bar on the back of the controller.
        /// </summary>
        /// <param name="color">Color to use for the light bar. Alpha component is ignored. Also,
        /// RBG values are clamped into [0..1] range.</param>
        void SetLightBarColor(Color color);
    }
}
