namespace UnityEngine.InputSystem.Processors
{
    /// <summary>
    /// Scale the components of a <see cref="Vector3"/> by constant factors.
    /// </summary>
    public class ScaleVector3Processor : InputProcessor<Vector3>
    {
        public float x = 1;
        public float y = 1;
        public float z = 1;

        public override Vector3 Process(Vector3 value, InputControl<Vector3> control)
        {
            return new Vector3(value.x * x, value.y * y, value.z * z);
        }
    }
}
