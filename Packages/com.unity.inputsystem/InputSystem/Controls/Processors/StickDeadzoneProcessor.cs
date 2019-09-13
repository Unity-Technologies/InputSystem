////REVIEW: rename to RadialDeadzone

////TODO: add different deadzone shapes and/or option to min/max X and Y separately

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Processes a Vector2 to apply deadzoning according to the magnitude of the vector (rather
    /// than just clamping individual axes). Normalizes to the min/max range.
    /// </summary>
    [Scripting.Preserve]
    public class StickDeadzoneProcessor : InputProcessor<Vector2>
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

        private float minOrDefault => min == default ? InputSystem.settings.defaultDeadzoneMin : min;
        private float maxOrDefault => max == default ? InputSystem.settings.defaultDeadzoneMax : max;

        public override Vector2 Process(Vector2 value, InputControl control = null)
        {
            var magnitude = value.magnitude;
            var newMagnitude = GetDeadZoneAdjustedValue(magnitude);
            if (newMagnitude == 0)
                value = Vector2.zero;
            else
                value *= newMagnitude / magnitude;
            return value;
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
