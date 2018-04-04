using System;
using UnityEngine.Experimental.Input.LowLevel;

////REVIEW: should we keep an explicit playback status? ATM calling ResumeHaptics() will re-issue last set motor speed regardless of pause state

namespace UnityEngine.Experimental.Input.Haptics
{
    /// <summary>
    /// Common implementation of dual motor rumbling.
    /// </summary>
    public struct DualMotorRumble
    {
        public float lowFrequencyMotorSpeed { get; private set; }
        public float highFrequencyMotorSpeed { get; private set; }

        public bool isRumbling
        {
            get
            {
                return !Mathf.Approximately(lowFrequencyMotorSpeed, 0f)
                    || !Mathf.Approximately(highFrequencyMotorSpeed, 0f);
            }
        }

        public void PauseHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (!isRumbling)
                return;

            var command = DualMotorRumbleCommand.Create(0f, 0f);
            device.ExecuteCommand(ref command);
        }

        public void ResumeHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (!isRumbling)
                return;

            SetMotorSpeeds(device, lowFrequencyMotorSpeed, highFrequencyMotorSpeed);
        }

        public void ResetHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (!isRumbling)
                return;

            SetMotorSpeeds(device, 0.0f, 0.0f);
        }

        public void SetMotorSpeeds(InputDevice device, float lowFrequency, float highFrequency)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            lowFrequencyMotorSpeed = Mathf.Clamp(lowFrequency, 0.0f, 1.0f);
            highFrequencyMotorSpeed = Mathf.Clamp(highFrequency, 0.0f, 1.0f);

            var command = DualMotorRumbleCommand.Create(lowFrequencyMotorSpeed, highFrequencyMotorSpeed);
            device.ExecuteCommand(ref command);
        }
    }
}
