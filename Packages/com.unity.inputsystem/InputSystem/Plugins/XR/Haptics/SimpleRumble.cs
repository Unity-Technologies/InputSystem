namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    /// <summary>
    /// This class controls the intensity and state of a rumble motor on a single XR device.
    /// </summary>
    public struct SimpleRumble
    {
        InputDevice device { get; set; }

        float m_Intensity;
        /// <summary>
        /// Determines the rumble intensity.  This expects a 0-1 value, where 0 is off, and 1 is the maximum amplitude available to the device
        /// </summary>
        public float intensity
        {
            get
            {
                return m_Intensity;
            }
            set
            {
                if (m_Intensity != value)
                {
                    m_Intensity = Mathf.Clamp(value, 0f, 1f);
                    UpdateMotorSpeed();
                }
            }
        }

        bool m_IsPaused;
        /// <summary>
        /// This allows you to pause the actual device motors.  This doesn't affect the current intensity, but prevents that intensity from being sent to the device.
        /// </summary>
        public bool isPaused
        {
            get
            {
                return m_IsPaused;
            }
            set
            {
                if (m_IsPaused != value)
                {
                    m_IsPaused = value;
                    UpdateMotorSpeed();
                }
            }
        }

        /// <summary>
        /// A quick accessor to verify that the intensity is greater than 0, and that the rumble motor is not paused.
        /// </summary>
        public bool isRumbling
        {
            get
            {
                return !m_IsPaused && !Mathf.Approximately(intensity, 0f);
            }
        }

        /// <summary>
        /// Resets the rumble motor state to defaults, which is an intensity of 0 and unpaused.
        /// </summary>
        public void Reset()
        {
            m_IsPaused = false;
            m_Intensity = 0f;
            UpdateMotorSpeed();
        }

        /// <summary>
        /// Simple constructor that links this SimpleRumble class to a specific device.
        /// </summary>
        /// <param name="device">The XR device containing the rumble motor you want to link to.</param>
        public SimpleRumble(InputDevice device)
        {
            this.device = device;

            m_IsPaused = false;
            m_Intensity = 0f;
        }

        private void UpdateMotorSpeed()
        {
            float intensity = m_IsPaused ? 0f : m_Intensity;
            var command = SendSimpleRumbleCommand.Create(intensity);
            device.ExecuteCommand(ref command);
        }
    }
}
