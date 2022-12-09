using UnityEngine;

namespace InputSamples.Gestures
{
    /// <summary>
    /// Simple object to contain information for a tap input.
    /// </summary>
    public struct TapInput
    {
        /// <summary>
        /// Position that the tap started.
        /// </summary>
        public readonly Vector2 PressPosition;

        /// <summary>
        /// Position that the tap released on.
        /// </summary>
        public readonly Vector2 ReleasePosition;

        /// <summary>
        /// How long the tap was held.
        /// </summary>
        public readonly double TapDuration;

        /// <summary>
        /// Total amount of drift the tap had, in screen units.
        /// </summary>
        public readonly float TapDrift;

        /// <summary>
        /// Timestamp of tap.
        /// </summary>
        public readonly double TimeStamp;

        internal TapInput(ActiveGesture gesture) : this()
        {
            PressPosition = gesture.StartPosition;
            ReleasePosition = gesture.EndPosition;
            TapDuration = gesture.EndTime - gesture.StartTime;
            TapDrift = gesture.TravelDistance;
            TimeStamp = gesture.EndTime;
        }
    }
}
