#if UNITY_WEBGL || UNITY_EDITOR
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.WebGL
{
    public static class WebGLSupport
    {
        public static void Initialize()
        {
            // We only turn gamepads with the "standard" mapping into actual Gamepads.
            InputSystem.RegisterLayout<WebGLGamepad>(
                matches: new InputDeviceMatcher()
                    .WithInterface("WebGL")
                    .WithDeviceClass("Gamepad")
                    .WithCapability("mapping", "standard"));

            ////TODO: add a layout builder for this that dynamically looks at the axis and button count
            // For all other WebGL "gamepads", we don't know the button and axis mappings so we turn
            // them into joysticks.
            InputSystem.RegisterLayout<WebGLJoystick>(
                matches: new InputDeviceMatcher()
                    .WithInterface("WebGL")
                    .WithDeviceClass("Gamepad"));
        }
    }
}
#endif // UNITY_WEBGL || UNITY_EDITOR
