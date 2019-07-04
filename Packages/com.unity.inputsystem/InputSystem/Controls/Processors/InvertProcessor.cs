namespace UnityEngine.InputSystem.Processors
{
    internal class InvertProcessor : InputProcessor<float>
    {
        public override float Process(float value, InputControl<float> control)
        {
            return value * -1.0f;
        }
    }
}
