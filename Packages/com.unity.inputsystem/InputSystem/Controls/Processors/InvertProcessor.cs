namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class InvertProcessor : InputProcessor<float>
    {
        public override float Process(float value, InputControl control)
        {
            return value * -1.0f;
        }
    }
}
