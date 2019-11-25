namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class InvertVector2Processor : InputProcessor<Vector2>
    {
        public bool invertX = true;
        public bool invertY = true;

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            if (invertX)
                value.x *= -1;
            if (invertY)
                value.y *= -1;
            return value;
        }
    }
}
