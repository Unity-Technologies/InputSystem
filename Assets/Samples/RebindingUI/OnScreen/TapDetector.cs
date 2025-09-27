using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    internal class TapDetector : ITouchProcessor
    {
        private Vector2 m_InitialPosition;
        private double m_InitialTime;
        private readonly float m_ThresholdSqr;
        private bool m_Valid;

        public TapDetector(float threshold)
        {
            m_ThresholdSqr = threshold * threshold;
            m_InitialPosition = Vector2.zero;
            m_InitialTime = 0;
            m_Valid = true;
        }

        public void OnTouchBegin(Detector context, in TouchState[] touches, int count, int index)
        {
            if (count == 1)
            {
                m_InitialPosition = touches[index].position;
                m_InitialTime = Time.realtimeSinceStartupAsDouble;
                m_Valid = true;
            }
            else
            {
                m_Valid = false;
            }
        }

        public void OnTouchEnd(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid && IsWithinMargin(touches[index].position))
            {
                GestureEvent @event = new GestureEvent(
                    delta: touches[index].position - m_InitialPosition,
                    duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                    flags: GestureEvent.Flags.TapGesture, start: m_InitialPosition);
                context.FireEvent(in @event);
            }
        }

        public void OnTouchMoved(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid && !IsWithinMargin(touches[index].position))
                m_Valid = false;
        }

        public void OnTouchCanceled(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
                m_Valid = false;
        }

        private bool IsWithinMargin(Vector2 point)
        {
            var sqrMagnitude = (point - m_InitialPosition).sqrMagnitude;
            return sqrMagnitude <= m_ThresholdSqr;
        }
    }
}
