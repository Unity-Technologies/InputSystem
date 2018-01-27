using System;
using ISX.LowLevel;
using UnityEngine;

namespace ISX.Haptics
{
    /// <summary>
    /// Common implementation of dual motor rumbling.
    /// </summary>
    public struct DualMotorRumble
    {
        public float lowFrequencyMotorSpeed { get; private set; }
        public float highFrequencyMotorSpeed { get; private set; }

        public void PauseHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // If not rumbling, don't send unnecessary command.
            if (Mathf.Approximately(lowFrequencyMotorSpeed, 0f)
                && Mathf.Approximately(highFrequencyMotorSpeed, 0f))
                return;

            var command = DualMotorRumbleCommand.Create(0f, 0f);
            device.OnDeviceCommand(ref command);
        }

        public void ResumeHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // If not rumbling, don't send unnecessary command.
            if (Mathf.Approximately(lowFrequencyMotorSpeed, 0f)
                && Mathf.Approximately(highFrequencyMotorSpeed, 0f))
                return;

            SetMotorSpeeds(device, lowFrequencyMotorSpeed, highFrequencyMotorSpeed);
        }

        public void SetMotorSpeeds(InputDevice device, float lowFrequency, float highFrequency)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            lowFrequencyMotorSpeed = Mathf.Clamp(lowFrequency, 0.0f, 1.0f);
            highFrequencyMotorSpeed = Mathf.Clamp(highFrequency, 0.0f, 1.0f);

            var command = DualMotorRumbleCommand.Create(lowFrequencyMotorSpeed, highFrequencyMotorSpeed);
            device.OnDeviceCommand(ref command);
        }
    }
}
