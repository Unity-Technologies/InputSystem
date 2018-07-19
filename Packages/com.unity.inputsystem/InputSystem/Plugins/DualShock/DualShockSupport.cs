namespace UnityEngine.Experimental.Input.Plugins.DualShock
{
    /// <summary>
    /// Adds support for PS4 DualShock controllers.
    /// </summary>
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<DualShockGamepad>();

            // HID version for platforms where we pick up the controller as a raw HID.
            // This works without any PS4-specific drivers but does not support the full
            // range of capabilities of the controller (the HID format is undocumented
            // and only partially understood).
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
            InputSystem.RegisterControlLayout<DualShockGamepadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("Sony.+Entertainment")
                    .WithProduct("Wireless Controller"));
            #endif

            #if UNITY_EDITOR || UNITY_PS4
            InputSystem.RegisterControlLayout<PS4TouchControl>("PS4Touch");
            InputSystem.RegisterControlLayout<DualShockGamepadPS4>("PS4DualShockGamepad",
                matches: new InputDeviceMatcher()
                    .WithInterface("PS4")
                    .WithDeviceClass("PS4DualShockGamepad"));
            #endif
        }
    }
}
