using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Processors
{
    public class CompensateDirectionProcessor : InputProcessor<Vector3>
    {
        public override Vector3 Process(Vector3 value, InputControl<Vector3> control)
        {
            if (!InputSystem.settings.compensateForScreenOrientation)
                return value;

            var rotation = Quaternion.identity;
            switch (InputRuntime.s_Instance.screenOrientation)
            {
                case ScreenOrientation.PortraitUpsideDown: rotation = Quaternion.Euler(0, 0, 180); break;
                case ScreenOrientation.LandscapeLeft: rotation = Quaternion.Euler(0, 0, 90); break;
                case ScreenOrientation.LandscapeRight: rotation = Quaternion.Euler(0, 0, 270); break;
            }
            return rotation * value;
        }
    }
}
