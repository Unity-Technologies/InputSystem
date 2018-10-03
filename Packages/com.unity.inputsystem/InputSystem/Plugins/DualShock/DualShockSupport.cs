using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.DualShock
{
    /// <summary>
    /// Adds support for PS4 DualShock controllers.
    /// </summary>
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<DualShockGamepad>();

            // HID version for platforms where we pick up the controller as a raw HID.
            // This works without any PS4-specific drivers but does not support the full
            // range of capabilities of the controller (the HID format is undocumented
            // and only partially understood).
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
            InputSystem.RegisterLayout<DualShockGamepadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("Sony.+Entertainment")
                    .WithProduct("Wireless Controller"));
            #endif

            ////TODO: make this work side-by-side with the other profile so that we can have this
            ////      active in UNITY_EDITOR; having tests that are active only on a specific platform
            ////      is a PITA
            // The "Manufacturer" field is not available in UWP (for some reason).
            // Identify PS4 controller by Sony's VendorID (VID).
            #if UNITY_WSA
            InputSystem.RegisterLayout<DualShockGamepadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x054c)
                    .WithProduct("Wireless Controller"));
            #endif

            #if UNITY_EDITOR || UNITY_PS4
            InputSystem.RegisterLayout<PS4TouchControl>("PS4Touch");
            InputSystem.RegisterLayout<DualShockGamepadPS4>("PS4DualShockGamepad",
                matches: new InputDeviceMatcher()
                    .WithInterface("PS4")
                    .WithDeviceClass("PS4DualShockGamepad"));
            #endif
        }
    }
}
