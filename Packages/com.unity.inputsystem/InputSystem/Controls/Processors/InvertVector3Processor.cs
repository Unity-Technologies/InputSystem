namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class InvertVector3Processor : InputProcessor<Vector3>
    {
        public bool invertX = true;
        public bool invertY = true;
        public bool invertZ = true;

        public override Vector3 Process(Vector3 value, InputControl control)
        {
            if (invertX)
                value.x *= -1;
            if (invertY)
                value.y *= -1;
            if (invertZ)
                value.z *= -1;
            return value;
        }
    }
}
