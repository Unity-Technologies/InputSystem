namespace ISX.DualShock
{
    /// <summary>
    /// Adds support for PS4 DualShock controllers.
    /// </summary>
    [InputPlugin]
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<DualShockGamepad>();

            // HID version for platforms where we pick up the controller as a raw HID.
            // This works without any PS4-specific drivers.
            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR
            InputSystem.RegisterTemplate<DualShockGamepadHID>(deviceDescription: new InputDeviceDescription
            {
                manufacturer = "Sony Interactive Entertainment",
                product = "Wireless Controller",
                interfaceName = "HID"
            });
            #endif
        }
    }
}
