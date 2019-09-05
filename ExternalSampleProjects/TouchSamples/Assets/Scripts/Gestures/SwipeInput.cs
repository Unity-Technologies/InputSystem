using UnityEngine;

namespace InputSamples.Gestures
{
    /// <summary>
    /// Simple object to contain information for a swipe input.
    /// </summary>
    public struct SwipeInput
    {
        /// <summary>
        /// ID of input that performed this swipe.
        /// </summary>
        public readonly int InputId;

        /// <summary>
        /// Position that the swipe began.
        /// </summary>
        public readonly Vector2 StartPosition;

        /// <summary>
        /// Last position that this swipe was at.
        /// </summary>
        public readonly Vector2 PreviousPosition;

        /// <summary>
        /// End position of the swipe.
        /// </summary>
        public readonly Vector2 EndPosition;

        /// <summary>
        /// Average normalized direction of the swipe. This is equivalent to
        /// <c>(EndPosition - StartPosition).normalized</c>.
        /// </summary>
        public readonly Vector2 SwipeDirection;

        /// <summary>
        /// Average velocity of the swipe in screen units per second.
        /// </summary>
        public readonly float SwipeVelocity;

        /// <summary>
        /// How much the swipe travelled in screen units. Will always be at least the difference between
        /// <see cref="StartPosition"/> and <see cref="EndPosition"/>, but will be longer for non-straight lines.
        /// </summary>
        public readonly float TravelDistance;

        /// <summary>
        /// Duration of the swipe in seconds.
        /// </summary>
        public readonly double SwipeDuration;

        /// <summary>
        /// A normalized measure of how consistent this swipe was in direction.
        /// </summary>
        public readonly float SwipeSameness;

        /// <summary>
        /// Construct a new swipe input from a given gesture.
        /// </summary>
        internal SwipeInput(ActiveGesture gesture) : this()
        {
            InputId = gesture.InputId;
            StartPosition = gesture.StartPosition;
            PreviousPosition = gesture.PreviousPosition;
            EndPosition = gesture.EndPosition;
            SwipeDirection = (EndPosition - StartPosition).normalized;
            SwipeDuration = gesture.EndTime - gesture.StartTime;
            TravelDistance = gesture.TravelDistance;
            SwipeSameness = gesture.SwipeDirectionSameness;

            if (SwipeDuration > 0.0f)
            {
                SwipeVelocity = (float)(TravelDistance / SwipeDuration);
            }
        }
    }
}
