using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.PS4
{
    /// <summary>
    /// Adds support for PS4 controllers.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class PS4Support
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
