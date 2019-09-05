using UnityEngine;

namespace InputSamples.Gestures
{
    /// <summary>
    /// An in-progress potential gesture for given input.
    /// </summary>
    internal sealed class ActiveGesture
    {
        /// <summary>
        /// Input ID that generated this gesture.
        /// </summary>
        public int InputId;

        /// <summary>
        /// The time this potential gesture started.
        /// </summary>
        public readonly double StartTime;

        /// <summary>
        /// The time this potential gesture ended.
        /// </summary>
        public double EndTime;

        /// <summary>
        /// The position this gesture started at.
        /// </summary>
        public readonly Vector2 StartPosition;

        /// <summary>
        /// The position this gesture was at during the last sample.
        /// </summary>
        public Vector2 PreviousPosition;

        /// <summary>
        /// The position this gesture ended at.
        /// </summary>
        public Vector2 EndPosition;

        /// <summary>
        /// How many samples we had for this gesture.
        /// </summary>
        public int Samples;

        /// <summary>
        /// How consistent the swipe was in its direction. Approaches 1 for straight lines.
        /// </summary>
        /// <remarks>
        /// This is calculated as the average of the dot products of every line segment (normalized) against a normalized
        /// vector to the tip of the swipe from the start.
        /// </remarks>
        public float SwipeDirectionSameness;

        /// <summary>
        /// The total travel distance this gesture's made in screen units. This will always be AT LEAST the distance
        /// between <see cref="StartPosition"/> and <see cref="EndPosition"/>, but will likely be longer for any
        /// non straight line gestures.
        /// </summary>
        public float TravelDistance;

        /// <summary>
        /// Accumulated sum of all normalized movement vectors.
        /// </summary>
        private Vector2 accumulatedNormalized;

        /// <summary>
        /// Instantiate a new potential gesture.
        /// </summary>
        /// <param name="inputId">The input id for this gesture.</param>
        /// <param name="startPosition">The gesture's start position.</param>
        /// <param name="startTime">The time the gesture has started.</param>
        public ActiveGesture(int inputId, Vector2 startPosition, double startTime)
        {
            InputId = inputId;
            EndTime = StartTime = startTime;
            EndPosition = StartPosition = startPosition;
            Samples = 1;
            SwipeDirectionSameness = 1;
            accumulatedNormalized = Vector2.zero;
        }

        /// <summary>
        /// Submit a new position to this gesture.
        /// </summary>
        /// <param name="position">The position of the new sample.</param>
        /// <param name="time">The time of the new sample.</param>
        public void SubmitPoint(Vector2 position, double time)
        {
            Vector2 toNewPosition = position - EndPosition;
            float distanceMoved = toNewPosition.magnitude;

            // Set new end time
            EndTime = time;

            if (Mathf.Approximately(distanceMoved, 0))
            {
                // Skipping point that is in the same position as the last one
                return;
            }

            // Normalize
            toNewPosition /= distanceMoved;

            Samples++;
            Vector2 toNewEndPosition = (position - StartPosition).normalized;

            // Set new end position and previous positions
            PreviousPosition = EndPosition;
            EndPosition = position;

            accumulatedNormalized += toNewPosition;

            SwipeDirectionSameness = Vector2.Dot(toNewEndPosition, accumulatedNormalized / (Samples - 1));

            TravelDistance += distanceMoved;
        }
    }
}
