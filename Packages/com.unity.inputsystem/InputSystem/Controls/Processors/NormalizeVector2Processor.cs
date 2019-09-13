namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class NormalizeVector2Processor : InputProcessor<Vector2>
    {
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            return value.normalized;
        }
    }
}
