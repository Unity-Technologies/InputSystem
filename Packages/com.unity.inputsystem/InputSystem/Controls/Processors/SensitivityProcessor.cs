using UnityEngine.Experimental.Input.LowLevel;

////TODO: ideally, also take pointer DPI into account

namespace UnityEngine.Experimental.Input.Processors
{
    public class SensitivityProcessor : IInputControlProcessor<Vector2>
    {
        public float sensitivity;

        public float sensitivityOrDefault
        {
            get { return sensitivity == 0f ? InputConfiguration.PointerDeltaSensitivity : sensitivity; }
        }

        public Vector2 Process(Vector2 value, InputControl control)
        {
            // Query dimensions of device.
            var dimensions = new Vector2(1f, 1f);
            var device = control.device;
            var command = QueryDimensionsCommand.Create();
            if (device.ExecuteCommand(ref command) >= 0)
                dimensions = command.outDimensions;

            // Scale X and Y.
            var sensitivityValue = sensitivityOrDefault;
            return new Vector2(value.x / dimensions.x * sensitivityValue, value.y / dimensions.y * sensitivityValue);
        }
    }
}
