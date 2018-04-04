namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    public struct SimpleXRRumble
    {
        public InputDevice device { get; private set; }

        float m_Intensity;
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
                    m_Intensity = value;
                    UpdateMotorSpeed();
                }
            }
        }

        bool m_IsPaused;
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

        public bool isRumbling
        {
            get
            {
                return !m_IsPaused && !Mathf.Approximately(intensity, 0f);
            }
        }

        public void Reset()
        {
            m_IsPaused = false;
            m_Intensity = 0f;
            UpdateMotorSpeed();
        }

        public SimpleXRRumble(InputDevice device)
        {
            this.device = device;

            m_IsPaused = false;
            m_Intensity = 0f;
        }

        private void UpdateMotorSpeed()
        {
            float intensity = m_IsPaused ? 0f : m_Intensity;
            var command = SimpleXRRumbleCommand.Create(intensity);
            device.ExecuteCommand(ref command);
        }
    }
}
