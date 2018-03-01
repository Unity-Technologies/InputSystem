using UnityEngine;

namespace ISX.Processors
{
    public class ClampProcessor : IInputProcessor<float>
    {
        public float min;
        public float max;

        public float Process(float value, InputControl control)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
