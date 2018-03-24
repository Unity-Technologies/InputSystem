namespace UnityEngine.Experimental.Input.Haptics
{
    /// <summary>
    /// A simple haptics interface that allows to control two motors individually.
    /// </summary>
    public interface IDualMotorRumble : IHaptics
    {
        /// <summary>
        /// Set the motor speeds of the left and right motor.
        /// </summary>
        /// <param name="lowFrequency">Speed of the low-frequency (left) motor. Normalized [0..1] value
        /// with 1 indicating maximum speed and 0 indicating the motor is turned off.</param>
        /// <param name="highFrequency">Speed of the high-frequency (right) motor. Normalized [0..1] value
        /// with 1 indicating maximum speed and 0 indicating the motor is turned off.</param>
        /// <remarks>
        /// Note that hardware will put limits on the level of control you have over the motors.
        /// Rumbling the motors at maximum speed for an extended period of time may cause them to turn
        /// off for some time to prevent overheating. Also, how quickly the motors react and how often
        /// the speed can be updated will depend on the hardware and drivers.
        /// </remarks>
        void SetMotorSpeeds(float lowFrequency, float highFrequency);
    }
}
