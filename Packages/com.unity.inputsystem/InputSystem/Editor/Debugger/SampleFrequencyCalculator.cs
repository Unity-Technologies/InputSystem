using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A utility for computing sample frequency based on input events.
    /// </summary>
    internal struct SampleFrequencyCalculator
    {
        private double m_LastUpdateTime;
        private int m_SampleCount;

        public SampleFrequencyCalculator(float targetFrequency, double realtimeSinceStartup)
        {
            this.targetFrequency = targetFrequency;
            this.m_SampleCount = 0;
            this.frequency = 0.0f;
            this.m_LastUpdateTime = realtimeSinceStartup;
        }

        public float targetFrequency { get; private set; }
        public float frequency { get; private set; }

        public void ProcessSample(InputEventPtr eventPtr)
        {
            // Only count actual samples instead of device-state changes which may be reported anyway it seems.
            // For determining frequency we at least absolute do not want to count state changes not driven
            // by an associated event/sample.
            if (eventPtr != null)
                ++m_SampleCount;
        }

        public bool Update() => Update(Time.realtimeSinceStartupAsDouble);

        public bool Update(double realtimeSinceStartup)
        {
            var timeSinceLastUpdate = realtimeSinceStartup - m_LastUpdateTime;
            if (timeSinceLastUpdate < 1.0)
                return false; // Only update once per second (and avoid division by zero)

            m_LastUpdateTime = realtimeSinceStartup;
            frequency = (float)(m_SampleCount / timeSinceLastUpdate);
            m_SampleCount = 0;

            return true;
        }
    }
}
