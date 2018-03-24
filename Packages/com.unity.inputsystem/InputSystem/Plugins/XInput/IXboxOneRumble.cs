using UnityEngine.Experimental.Input.Haptics;

namespace UnityEngine.Experimental.Input.Plugins.XInput
{
    /// <summary>
    /// Extended dual motor gamepad rumble that adds left and right trigger motors.
    /// </summary>
    public interface IXboxOneRumble : IDualMotorRumble
    {
        void SetMotorSpeeds(float lowFrequency, float highFrequency, float leftTrigger, float rightTrigger);
    }
}
