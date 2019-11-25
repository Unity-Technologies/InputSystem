////TODO: move clamping settings into struct and add process function; then embed both here and in AxisControl

namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class ClampProcessor : InputProcessor<float>
    {
        public float min;
        public float max;

        public override float Process(float value, InputControl control)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
