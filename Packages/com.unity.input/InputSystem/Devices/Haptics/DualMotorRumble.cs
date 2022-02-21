using System;
using UnityEngine.InputSystem.LowLevel;

////REVIEW: should we keep an explicit playback status? ATM calling ResumeHaptics() will re-issue last set motor speed regardless of pause state

namespace UnityEngine.InputSystem.Haptics
{
    /// <summary>
    /// Common implementation of dual motor rumbling.
    /// </summary>
    /// <remarks>
    /// This struct is meant for use in devices that implement <see cref="IDualMotorRumble"/>.
    /// </remarks>
    internal struct DualMotorRumble
    {
        /// <summary>
        /// Normalized [0..1] speed of the low-frequency (usually left) motor.
        /// </summary>
        /// <value>Speed of left motor.</value>
        public float lowFrequencyMotorSpeed { get; private set; }

        /// <summary>
        /// Normalized [0..1] speed of the high-frequency (usually right) motor.
        /// </summary>
        /// <value>Speed of right motor.</value>
        public float highFrequencyMotorSpeed { get; private set; }

        /// <summary>
        /// Whether either of the motors is currently set to non-zero speeds.
        /// </summary>
        /// <value>True if the motors are currently turned on.</value>
        /// <remarks>
        /// Does not take pausing into account, i.e. <see cref="lowFrequencyMotorSpeed"/> and/or
        /// <see cref="highFrequencyMotorSpeed"/> may be non-zero but haptics on the device
        /// may actually be paused with <see cref="PauseHaptics"/>.
        /// </remarks>
        public bool isRumbling =>
            !Mathf.Approximately(lowFrequencyMotorSpeed, 0f)
            || !Mathf.Approximately(highFrequencyMotorSpeed, 0f);

        /// <summary>
        /// Reset motor speeds to zero but retain current values for <see cref="lowFrequencyMotorSpeed"/>
        /// and <see cref="highFrequencyMotorSpeed"/>.
        /// </summary>
        /// <param name="device">Device to send command to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        public void PauseHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (!isRumbling)
                return;

            var command = DualMotorRumbleCommand.Create(0f, 0f);
            device.ExecuteCommand(ref command);
        }

        /// <summary>
        /// Resume haptics by setting motor speeds to the current values of <see cref="lowFrequencyMotorSpeed"/>
        /// and <see cref="highFrequencyMotorSpeed"/>.
        /// </summary>
        /// <param name="device">Device to send command to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        public void ResumeHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (!isRumbling)
                return;

            SetMotorSpeeds(device, lowFrequencyMotorSpeed, highFrequencyMotorSpeed);
        }

        /// <summary>
        /// Reset haptics by setting both <see cref="lowFrequencyMotorSpeed"/> and <see cref="highFrequencyMotorSpeed"/>
        /// to zero.
        /// </summary>
        /// <param name="device">Device to send command to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        public void ResetHaptics(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (!isRumbling)
                return;

            SetMotorSpeeds(device, 0.0f, 0.0f);
        }

        /// <summary>
        /// Set the speed of the low-frequency (usually left) and high-frequency (usually right) motor
        /// on <paramref name="device"/>. Updates <see cref="lowFrequencyMotorSpeed"/> and
        /// <see cref="highFrequencyMotorSpeed"/>.
        /// </summary>
        /// <param name="device">Device to send command to.</param>
        /// <param name="lowFrequency">Speed of the low-frequency (left) motor. Normalized [0..1] value
        /// with 1 indicating maximum speed and 0 indicating the motor is turned off. Will automatically
        /// be clamped into range.</param>
        /// <param name="highFrequency">Speed of the high-frequency (right) motor. Normalized [0..1] value
        /// with 1 indicating maximum speed and 0 indicating the motor is turned off. Will automatically
        /// be clamped into range.</param>
        /// <remarks>
        /// Sends <see cref="DualMotorRumbleCommand"/> to <paramref name="device"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
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
