using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale a float value by a constant factor.
    /// </summary>
    [Preserve]
    internal class ScaleProcessor : InputProcessor<float>
    {
        [Tooltip("Scale factor to multiply incoming float values by.")]
        public float factor = 1;

        public override float Process(float value, InputControl control)
        {
            return value * factor;
        }
    }
}
