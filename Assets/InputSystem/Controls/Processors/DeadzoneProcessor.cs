using UnityEngine;

namespace ISX
{
    // Processes a Vector2 to apply deadzoning according to the magnitude of the vector (rather
    // than just clamping individual axes).
    // Normalizes to the min/max range.
    public class DeadzoneProcessor : IInputProcessor<Vector2>
    {
        public float min = 0.225f;
        public float max = 0.925f;

        public Vector2 Process(Vector2 vector)
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
            var absValue = Mathf.Abs(value);
            if (absValue < min)
                return 0;
            if (absValue > max)
                return Mathf.Sign(value);
            return Mathf.Sign(value) * ((absValue - min) / (max - min));
        }
    }
}
