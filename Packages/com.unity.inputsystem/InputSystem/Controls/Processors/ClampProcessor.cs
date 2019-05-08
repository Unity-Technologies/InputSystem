////TODO: move clamping settings into struct and add process function; then embed both here and in AxisControl

namespace UnityEngine.InputSystem.Processors
{
    public class ClampProcessor : InputProcessor<float>
    {
        public float min;
        public float max;

        public override float Process(float value, InputControl<float> control)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
