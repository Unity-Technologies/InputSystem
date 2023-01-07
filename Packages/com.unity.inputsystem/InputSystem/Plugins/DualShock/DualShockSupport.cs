using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.DualShock
{
    /// <summary>
    /// Adds support for PS4 DualShock controllers.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterLayout<DualShockGamepad>();

            // HID version for platforms where we pick up the controller as a raw HID.
            // This works without any PS4-specific drivers but does not support the full
            // range of capabilities of the controller (the HID format is undocumented
            // and only partially understood).
            //
            // NOTE: We match by PID and VID here as that is the most reliable way. The product
            //       and manufacturer strings we get from APIs often return inconsistent results
            //       or none at all. E.g. when connected via Bluetooth on OSX, the DualShock will
            //       not return anything from IOHIDDevice_GetProduct() and IOHIDevice_GetManufacturer()
            //       even though it will report the expected results when plugged in via USB.
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA || UNITY_EDITOR
            InputSystem.RegisterLayout<DualSenseGamepadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x54C) // Sony Entertainment.
                    .WithCapability("productId", 0xCE6));
            InputSystem.RegisterLayout<DualShock4GamepadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x54C) // Sony Entertainment.
                    .WithCapability("productId", 0x9CC)); // Wireless controller.
            InputSystem.RegisterLayoutMatcher<DualShock4GamepadHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x54C) // Sony Entertainment.
                    .WithCapability("productId", 0x5C4)); // Wireless controller.

            // Just to make sure, also set up a matcher that goes by strings so that we cover
            // all bases.
            InputSystem.RegisterLayoutMatcher<DualShock4GamepadHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("Sony.+Entertainment")
                    .WithProduct("Wireless Controller"));

            InputSystem.RegisterLayout<DualShock3GamepadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x54C) // Sony Entertainment.
                    .WithCapability("productId", 0x268)); // PLAYSTATION(R)3 Controller.

            InputSystem.RegisterLayoutMatcher<DualShock3GamepadHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithManufacturer("Sony.+Entertainment")
                    .WithProduct("PLAYSTATION(R)3 Controller"));
            #endif
        }
    }
}
