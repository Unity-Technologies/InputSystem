using UnityEngine.InputSystem.Haptics;

namespace UnityEngine.InputSystem.Plugins.Switch
{
    /// <summary>
    /// Extended dual motor gamepad that adds seperate controls for rumble frequency and amplitude.
    /// </summary>
    public interface INPadRumble : IDualMotorRumble
    {
        /// <summary>
        /// Set rumble for all motors
        /// </summary>
        void SetMotorSpeeds(float lowAmplitued, float lowFrequency, float highAmplitued, float highFrequency);

        /// <summary>
        /// Set rumble for left motor
        /// </summary>
        void SetMotorSpeedLeft(float lowAmplitued, float lowFrequency, float highAmplitued, float highFrequency);

        /// <summary>
        /// Set rumble for right motor
        /// </summary>
        void SetMotorSpeedRight(float lowAmplitued, float lowFrequency, float highAmplitued, float highFrequency);
    }
}
