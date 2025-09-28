using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A gesture detector processor for "drag" a.k.a. "translate/pan" gestures.
    /// </summary>
    internal class DragDetector : ITouchProcessor
    {
        private Vector2 m_InitialPosition;
        private double m_InitialTime;
        private readonly float m_ThresholdSqr;
        private bool m_Valid;

        public DragDetector(float threshold)
        {
            m_InitialTime = 0;
            m_InitialPosition = Vector2.zero;
            m_ThresholdSqr = threshold * threshold;
            m_Valid = false;
        }

        #region ITouchProcessor implementation

        /// <inheritdoc/>
        public void OnTouchBegin(Detector context, in TouchState[] touches, int count, int index)
        {
            if (count == 1)
            {
                m_InitialPosition = touches[index].position;
                m_InitialTime = Time.realtimeSinceStartupAsDouble;

                // Require that we move outside "dead zone" before we consider the drag to start.
                if (IsWithinMargin(touches[index].position))
                    return;

                m_Valid = true;
                Fire(context, touches, count, index, GestureEvent.Flags.PhaseStart);
            }
            else if (m_Valid)
            {
                m_Valid = false;
                Fire(context, touches, count, index, GestureEvent.Flags.PhaseCancel);
            }
        }

        /// <inheritdoc/>
        public void OnTouchEnd(Detector context, in TouchState[] touches, int count, int index)
        {
            if (!m_Valid)
                return;

            Fire(context, touches, count, index, GestureEvent.Flags.PhaseEnd);
            m_Valid = false;
        }

        /// <inheritdoc/>
        public void OnTouchMoved(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                Fire(context, touches, count, index, GestureEvent.Flags.PhaseChange);
            }
            else
            {
                // Require that we move outside "dead zone" before we consider the drag to start.
                if (IsWithinMargin(touches[index].position))
                    return;

                m_Valid = true;
                Fire(context, touches, count, index, GestureEvent.Flags.PhaseStart);
            }
        }

        /// <inheritdoc/>
        public void OnTouchCanceled(Detector context, in TouchState[] touches, int count, int index)
        {
            if (m_Valid)
            {
                m_Valid = false;
                Fire(context, touches, count, index, GestureEvent.Flags.PhaseCancel);
            }
        }

        #endregion

        private void Fire(Detector context, in TouchState[] touches, int count, int index, GestureEvent.Flags phase)
        {
            GestureEvent @event = new GestureEvent(
                delta: touches[index].position - m_InitialPosition,
                duration: Time.realtimeSinceStartupAsDouble - m_InitialTime,
                flags: GestureEvent.Flags.DragGesture | phase, start: m_InitialPosition);
            context.FireEvent(in @event);
        }

        private bool IsWithinMargin(Vector2 point)
        {
            var sqrMagnitude = (point - m_InitialPosition).sqrMagnitude;
            return sqrMagnitude < m_ThresholdSqr;
        }
    }
}
