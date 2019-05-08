namespace UnityEngine.InputSystem.Processors
{
    public class NormalizeVector2Processor : InputProcessor<Vector2>
    {
        public override Vector2 Process(Vector2 value, InputControl<Vector2> control)
        {
            return value.normalized;
        }
    }
}
