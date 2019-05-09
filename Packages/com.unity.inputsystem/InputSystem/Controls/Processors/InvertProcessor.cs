namespace UnityEngine.InputSystem.Processors
{
    public class InvertProcessor : InputProcessor<float>
    {
        public override float Process(float value, InputControl<float> control)
        {
            return value * -1.0f;
        }
    }
}
