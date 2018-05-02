////TODO: move clamping settings into struct and add process function; then embed both here and in AxisControl

namespace UnityEngine.Experimental.Input.Processors
{
    public class ClampProcessor : IInputControlProcessor<float>
    {
        public float min;
        public float max;

        public float Process(float value, InputControl control)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
