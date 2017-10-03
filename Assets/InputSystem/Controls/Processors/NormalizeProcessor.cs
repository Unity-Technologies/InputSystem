using UnityEngine;

namespace ISX
{
    // Normalizes input values in the range [min..max] to [0..1].
    public class NormalizeProcessor : IInputProcessor<float>
    {
        public float min;
        public float max;

        public float Process(float value)
        {
            return (Mathf.Clamp(value, min, max) - Mathf.Abs(min)) / (max - Mathf.Abs(min));
        }
    }
}
