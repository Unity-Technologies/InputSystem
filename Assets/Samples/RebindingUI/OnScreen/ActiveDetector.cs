using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    internal class ActiveDetector : ITouchProcessor
    {
        private Vector2 m_InitialPosition = Vector2.zero;
        private double m_InitialTime = 0;
        private bool m_Valid = true;

        public void OnTouchBegin(Detector context, in TouchState[] touches, int count, int index)
        {
            if (count == 1)
            {
                m_InitialPosition = touches[index].position;
                m_InitialTime = Time.realtimeSinceStartupAsDouble;
                m_Valid = true;

                GestureEvent @event = new GestureEvent(
                    delta: Vector2.zero,
                    duration: 0.0,
                    flags: GestureEvent.Flags.Active | GestureEvent.Flags.PhaseStart,
                    start: m_InitialPosition
                );
                context.FireEvent(in @event);
            }
            else
            {
                m_Valid = false;
            }
        }

        public void OnTouchEnd(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                GestureEvent @event = new GestureEvent(
                    delta: touches[index].position - m_InitialPosition,
                    duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                    flags: GestureEvent.Flags.Active | GestureEvent.Flags.PhaseEnd, start: m_InitialPosition);
                context.FireEvent(in @event);
            }
        }

        public void OnTouchMoved(Detector context, in TouchState[] touches, int count, int index)
        {
            // Ignored
        }

        public void OnTouchCanceled(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                m_Valid = false;

                var @event = new GestureEvent(
                    delta: touches[index].position - m_InitialPosition,
                    duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                    flags: GestureEvent.Flags.Active | GestureEvent.Flags.PhaseCancel,
                    start: m_InitialPosition);
                context.FireEvent(in @event);
            }
        }
    }
}
