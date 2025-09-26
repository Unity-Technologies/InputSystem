using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    class DragDetector : ITouchProcessor
    {
        private double m_InitialTime;
        private Vector2 m_InitialPosition;
        private readonly float m_ThresholdSqr;
        private bool m_Valid;

        public DragDetector(float threshold)
        {
            m_InitialTime = 0;
            m_InitialPosition = Vector2.zero;
            m_ThresholdSqr = threshold * threshold;
            m_Valid = false;
        }

        public void OnTouchBegin(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (count == 1)
            {
                m_InitialPosition = touches[index].position;
                m_InitialTime = Time.realtimeSinceStartupAsDouble;
            }
            else if (m_Valid)
            {
                m_Valid = false;
                Fire(context, touches, count, index, GestureFlags.PhaseCancel);
            }
        }

        public void OnTouchEnd(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (!m_Valid)
                return;

            Fire(context, touches, count, index, GestureFlags.PhaseEnd);
            m_Valid = false;
        }

        public void OnTouchMoved(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                Fire(context, touches, count, index, GestureFlags.PhaseChange);
            }
            else
            {
                // Require that we move outside "dead zone" before we consider the drag to start.
                if (IsWithinMargin(touches[index].position))
                    return;

                m_Valid = true;
                Fire(context, touches, count, index, GestureFlags.PhaseStart);
            }
        }

        public void OnTouchCanceled(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                m_Valid = false;
                Fire(context, touches, count, index, GestureFlags.PhaseCancel);
            }
        }

        private void Fire(ChangeMonitor context, in TouchState[] touches, int count, int index, GestureFlags phase)
        {
            GestureEvent @event = new GestureEvent(
                delta: touches[index].position - m_InitialPosition,
                duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                flags: GestureFlags.DragGesture | phase, start: m_InitialPosition);
            context.FireEvent(in @event);
        }

        private bool IsWithinMargin(Vector2 point)
        {
            var sqrMagnitude = (point - m_InitialPosition).sqrMagnitude;
            return sqrMagnitude <= m_ThresholdSqr;
        }
    }
}
