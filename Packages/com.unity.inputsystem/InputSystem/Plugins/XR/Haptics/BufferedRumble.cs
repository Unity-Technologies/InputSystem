namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    public struct BufferedRumble
    {
        public HapticCapabilities capabilities { get; private set; }
        InputDevice device { get; set; }

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
                return !m_IsPaused;
            }
        }

        /// <summary>
        /// Resets the rumble motor state to defaults, which is an intensity of 0 and unpaused.
        /// </summary>
        public void Reset()
        {
            m_IsPaused = false;
            UpdateMotorSpeed();
        }

        public BufferedRumble(InputDevice device)
        {
            this.device = device;

            var command = HapticCapabilitiesCommand.Create();
            device.ExecuteCommand(ref command);
            capabilities = command.Capabilities;

            m_IsPaused = false;
        }

        private void UpdateMotorSpeed()
        {
        }
    }
}
