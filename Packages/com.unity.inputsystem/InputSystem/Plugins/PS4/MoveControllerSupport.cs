using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.DualShock
{
    /// <summary>
    /// Adds support for PS4 DualShock controllers.
    /// </summary>
    public static class MoveControllerSupport
    {
        public static void Initialize()
        {
            #if UNITY_EDITOR || UNITY_PS4
            InputSystem.RegisterLayout<MoveControllerPS4>("PS4MoveController",
                matches: new InputDeviceMatcher()
                    .WithInterface("PS4")
                    .WithDeviceClass("PS4MoveController"));
            #endif
        }
    }
}
