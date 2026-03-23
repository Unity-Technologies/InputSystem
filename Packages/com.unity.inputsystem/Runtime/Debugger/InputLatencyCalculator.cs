#if UNITY_EDITOR
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A utility for computing latency based on input events.
    /// </summary>
    internal struct InputLatencyCalculator
    {
        private double m_LastUpdateTime;
        private double m_AccumulatedLatencySeconds;
        private double m_AccumulatedMinLatencySeconds;
        private double m_AccumulatedMaxLatencySeconds;
        private int m_SampleCount;

        public InputLatencyCalculator(double realtimeSinceStartup)
        {
            m_LastUpdateTime = realtimeSinceStartup;
            m_AccumulatedLatencySeconds = 0.0;
            m_AccumulatedMinLatencySeconds = 0.0;
            m_AccumulatedMaxLatencySeconds = 0.0;
            m_SampleCount = 0;
            averageLatencySeconds = float.NaN;
            minLatencySeconds = float.NaN;
            maxLatencySeconds = float.NaN;
        }

        public void ProcessSample(InputEventPtr eventPtr) =>
            ProcessSample(eventPtr, Time.realtimeSinceStartupAsDouble);

        public void ProcessSample(InputEventPtr eventPtr, double realtimeSinceStartup)
        {
            if (!eventPtr.valid)
                return;

            var ageInSeconds = realtimeSinceStartup - eventPtr.time;
            m_AccumulatedLatencySeconds += ageInSeconds;
            if (++m_SampleCount == 1)
            {
                m_AccumulatedMinLatencySeconds = ageInSeconds;
                m_AccumulatedMaxLatencySeconds = ageInSeconds;
            }
            else if (ageInSeconds < m_AccumulatedMaxLatencySeconds)
                m_AccumulatedMinLatencySeconds = ageInSeconds;
            else if (ageInSeconds > m_AccumulatedMaxLatencySeconds)
                m_AccumulatedMaxLatencySeconds = ageInSeconds;
        }

        public float averageLatencySeconds { get; private set; }
        public float minLatencySeconds { get; private set; }
        public float maxLatencySeconds { get; private set; }

        public bool Update() => Update(Time.realtimeSinceStartupAsDouble);

        public bool Update(double realtimeSinceStartup)
        {
            var timeSinceLastUpdate = realtimeSinceStartup - m_LastUpdateTime;
            if (timeSinceLastUpdate < 1.0)
                return false; // Only update once per second (and avoid division by zero)

            if (m_SampleCount == 0)
            {
                averageLatencySeconds = float.NaN;
                minLatencySeconds = float.NaN;
                maxLatencySeconds = float.NaN;
            }
            else
            {
                averageLatencySeconds = (float)(m_AccumulatedLatencySeconds / m_SampleCount);
                minLatencySeconds = (float)m_AccumulatedMinLatencySeconds;
                maxLatencySeconds = (float)m_AccumulatedMaxLatencySeconds;
            }

            m_LastUpdateTime = realtimeSinceStartup;
            m_SampleCount = 0;
            m_AccumulatedLatencySeconds = 0.0;

            return true;
        }
    }
}
#endif // UNITY_EDITOR
