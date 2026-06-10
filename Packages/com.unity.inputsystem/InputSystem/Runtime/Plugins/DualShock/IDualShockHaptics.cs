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
        /// <remarks>
        /// The light bar color persists on the controller hardware after the
        /// application exits and is not automatically restored to the default color.
        /// </remarks>
        void SetLightBarColor(Color color);
    }
}
