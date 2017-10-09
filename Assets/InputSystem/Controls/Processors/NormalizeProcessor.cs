using UnityEngine;

namespace ISX
{
    // Normalizes input values in the range [min..max] to unsigned normalized
    // form [0..1] if min is >= 0 and to signed normalized form [-1..1] if
    // min < 0.
    public class NormalizeProcessor : IInputProcessor<float>
    {
        public float min;
        public float max;

        public float Process(float value)
        {
            var minAbsolute = Mathf.Abs(min);
            return (value + minAbsolute) / (max + minAbsolute) - minAbsolute;
        }

        public static float Normalize(float value, float min, float max)
        {
            var minAbsolute = Mathf.Abs(min);
            var percentage = (value + minAbsolute) / (max + minAbsolute);
            if (min < 0.0f)
                return 2 * percentage - 1;
            return percentage;
        }
    }
}
