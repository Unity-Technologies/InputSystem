using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.PS4
{
    /// <summary>
    /// Adds support for PS4 controllers.
    /// </summary>
    public static class PS4Support
    {
        public static void Initialize()
        {
            #if UNITY_EDITOR || UNITY_PS4
            InputSystem.RegisterLayout<MoveControllerPS4>("PS4MoveController",
                matches: new InputDeviceMatcher()
                    .WithInterface("PS4")
                    .WithDeviceClass("PS4MoveController"));

            InputSystem.RegisterLayout<PS4TouchControl>("PS4Touch");
            InputSystem.RegisterLayout<DualShockGamepadPS4>("PS4DualShockGamepad",
                matches: new InputDeviceMatcher()
                    .WithInterface("PS4")
                    .WithDeviceClass("PS4DualShockGamepad"));
            #endif
        }
    }
}
