namespace UnityEngine.Experimental.Input.Processors
{
    /// <summary>
    /// Processes a Vector2 to apply deadzoning according to the magnitude of the vector (rather
    /// than just clamping individual axes). Normalizes to the min/max range.
    /// </summary>
    public class DeadzoneProcessor : IInputControlProcessor<Vector2>
    {
        /// <summary>
        /// Value at which the lower bound deadzone starts.
        /// </summary>
        /// <remarks>
        /// Values in the input at or below min will get dropped and values
        /// will be scaled to the range between min and max.
        /// </remarks>
        public float min;
        public float max;

        public float minOrDefault
        {
            get { return min == 0.0f ? InputConfiguration.DeadzoneMin : min; }
        }

        public float maxOrDefault
        {
            get { return max == 0.0f ? InputConfiguration.DeadzoneMax : max; }
        }

        public Vector2 Process(Vector2 vector, InputControl control)
        {
            var magnitude = vector.magnitude;
            var newMagnitude = GetDeadZoneAdjustedValue(magnitude);
            if (newMagnitude == 0)
                vector = Vector2.zero;
            else
                vector *= newMagnitude / magnitude;
            return vector;
        }

        private float GetDeadZoneAdjustedValue(float value)
        {
            var min = minOrDefault;
            var max = maxOrDefault;

            var absValue = Mathf.Abs(value);
            if (absValue < min)
                return 0;
            if (absValue > max)
                return Mathf.Sign(value);

            return Mathf.Sign(value) * ((absValue - min) / (max - min));
        }
    }
}
