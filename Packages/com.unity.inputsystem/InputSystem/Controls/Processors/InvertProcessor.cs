namespace UnityEngine.Experimental.Input.Processors
{
    public class InvertProcessor : IInputControlProcessor<float>
    {
        public float Process(float value, InputControl control)
        {
            return value * -1.0f;
        }
    }
}
