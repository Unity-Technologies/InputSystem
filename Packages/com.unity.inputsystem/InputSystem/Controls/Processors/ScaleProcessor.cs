namespace UnityEngine.Experimental.Input.Processors
{
    /// <summary>
    /// Scale a float value by a constant factor.
    /// </summary>
    public class ScaleProcessor : IInputControlProcessor<float>
    {
        [Tooltip("Scale factor to multiply incoming float values by.")]
        public float factor;

        public float Process(float value, InputControl control)
        {
            return value * factor;
        }
    }
}
