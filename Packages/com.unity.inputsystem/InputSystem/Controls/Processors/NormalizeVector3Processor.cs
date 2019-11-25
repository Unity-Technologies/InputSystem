namespace UnityEngine.InputSystem.Processors
{
    [Scripting.Preserve]
    internal class NormalizeVector3Processor : InputProcessor<Vector3>
    {
        public override Vector3 Process(Vector3 value, InputControl control)
        {
            return value.normalized;
        }
    }
}
