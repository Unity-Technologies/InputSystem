namespace UnityEngine.Experimental.Input.Processors
{
    // Normalizes input values in the range [min..max] to unsigned normalized
    // form [0..1] if min is >= 0 and to signed normalized form [-1..1] if
    // min < 0.
    public class NormalizeProcessor : IInputProcessor<float>
    {
        public float min;
        public float max;
        public float zero;

        public float Process(float value, InputControl control)
        {
            return Normalize(value, min, max, zero);
        }

        public static float Normalize(float value, float min, float max, float zero)
        {
            if (zero < min)
                zero = min;
            var minAbsolute = Mathf.Abs(min);
            var percentage = (value - minAbsolute) / (max - minAbsolute);
            if (min < zero)
                return 2 * percentage - 1;
            return percentage;
        }
    }
}
