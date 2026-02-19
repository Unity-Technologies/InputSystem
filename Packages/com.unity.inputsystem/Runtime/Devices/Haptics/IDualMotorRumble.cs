namespace UnityEngine.InputSystem.Haptics
{
    /// <summary>
    /// A simple haptics interface that allows to control two motors individually.
    /// </summary>
    /// <remarks>
    /// Dual-motor control is most common on gamepads (see <see cref="Gamepad"/>) such as
    /// Xbox and PlayStation controllers.
    /// </remarks>
    public interface IDualMotorRumble : IHaptics
    {
        /// <summary>
        /// Set the motor speeds of the low-frequency (usually on the left) and high-frequency
        /// (usually on the right) motors.
        /// </summary>
        /// <param name="lowFrequency">Speed of the low-frequency (left) motor. Normalized [0..1] value
        /// with 1 indicating maximum speed and 0 indicating the motor is turned off. Will automatically
        /// be clamped into range.</param>
        /// <param name="highFrequency">Speed of the high-frequency (right) motor. Normalized [0..1] value
        /// with 1 indicating maximum speed and 0 indicating the motor is turned off. Will automatically
        /// be clamped into range.</param>
        /// <remarks>
        /// Note that hardware will put limits on the level of control you have over the motors.
        /// Rumbling the motors at maximum speed for an extended period of time may cause them to turn
        /// off for some time to prevent overheating. Also, how quickly the motors react and how often
        /// the speed can be updated will depend on the hardware and drivers.
        /// </remarks>
        void SetMotorSpeeds(float lowFrequency, float highFrequency);
    }
}
