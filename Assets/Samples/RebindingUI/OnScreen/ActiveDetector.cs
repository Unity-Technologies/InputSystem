using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    class ActiveDetector : ITouchProcessor
    {
        private Vector2 m_InitialPosition = Vector2.zero;
        private double m_InitialTime = 0;
        private bool m_Valid = true;

        public void OnTouchBegin(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (count == 1)
            {
                m_InitialPosition = touches[index].position;
                m_InitialTime = Time.realtimeSinceStartupAsDouble;
                m_Valid = true;

                GestureEvent @event = new GestureEvent(
                    delta: Vector2.zero,
                    duration: 0.0,
                    flags: GestureFlags.Active | GestureFlags.PhaseStart,
                    start: m_InitialPosition
                );
                context.FireEvent(in @event);
            }
            else
            {
                m_Valid = false;
            }
        }

        public void OnTouchEnd(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                GestureEvent @event = new GestureEvent(
                    delta: touches[index].position - m_InitialPosition,
                    duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                    flags: GestureFlags.Active | GestureFlags.PhaseEnd, start: m_InitialPosition);
                context.FireEvent(in @event);
            }
        }

        public void OnTouchMoved(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            // Ignored
        }

        public void OnTouchCanceled(ChangeMonitor context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                m_Valid = false;

                GestureEvent @event = new GestureEvent(
                    delta: touches[index].position - m_InitialPosition,
                    duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                    flags: GestureFlags.Active | GestureFlags.PhaseCancel,
                    start: m_InitialPosition);
                context.FireEvent(in @event);
            }
        }
    }
}
