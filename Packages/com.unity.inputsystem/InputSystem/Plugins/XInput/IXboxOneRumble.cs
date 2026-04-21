using UnityEngine.InputSystem.Haptics;

namespace UnityEngine.InputSystem.XInput
{
    /// <summary>
    /// Extended dual motor gamepad rumble that adds left and right trigger motors.
    /// </summary>
    public interface IXboxOneRumble : IDualMotorRumble
    {
        /// <summary>Sets the speeds of all four Xbox One rumble motors.</summary>
        void SetMotorSpeeds(float lowFrequency, float highFrequency, float leftTrigger, float rightTrigger);
    }
}
