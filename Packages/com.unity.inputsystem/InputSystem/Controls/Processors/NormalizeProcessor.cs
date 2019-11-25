using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    // Normalizes input values in the range [min..max] to unsigned normalized
    // form [0..1] if min is >= 0 and to signed normalized form [-1..1] if
    // min < 0.
    [Preserve]
    internal class NormalizeProcessor : InputProcessor<float>
    {
        public float min;
        public float max;
        public float zero;

        public override float Process(float value, InputControl control)
        {
            return Normalize(value, min, max, zero);
        }

        public static float Normalize(float value, float min, float max, float zero)
        {
            if (zero < min)
                zero = min;
            // Prevent NaN/Inf from dividing 0 by something.
            if (Mathf.Approximately(value, min))
            {
                if (min < zero)
                    return -1f;
                return 0f;
            }
            var percentage = (value - min) / (max - min);
            if (min < zero)
                return 2 * percentage - 1;
            return percentage;
        }
    }
}
