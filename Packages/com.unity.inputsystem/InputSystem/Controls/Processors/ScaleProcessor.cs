namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale a float value by a constant factor.
    /// </summary>
    [Scripting.Preserve]
    public class ScaleProcessor : InputProcessor<float>
    {
        [Tooltip("Scale factor to multiply incoming float values by.")]
        public float factor = 1;

        public override float Process(float value, InputControl<float> control)
        {
            return value * factor;
        }
    }
}
