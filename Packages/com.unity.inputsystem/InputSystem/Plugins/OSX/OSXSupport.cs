#if UNITY_EDITOR || UNITY_STANDALONE_OSX
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OSX.LowLevel;

namespace UnityEngine.InputSystem.OSX
{
    /// <summary>
    /// A small helper class to aid in initializing and registering HID device layout builders.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class OSXSupport
    {
        /// <summary>
        /// Registers HID device layouts for OSX.
        /// </summary>
        public static void Initialize()
        {
            // Note that OSX reports manufacturer "Unknown" and a bogus VID/PID according
            // to matcher below.
            InputSystem.RegisterLayout<NimbusGamepadHid>(
                matches: new InputDeviceMatcher()
                    .WithProduct("Nimbus+", supportRegex: false)
                    .WithCapability("vendorId", NimbusPlusHIDInputReport.OSXVendorId)
                    .WithCapability("productId", NimbusPlusHIDInputReport.OSXProductId));
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_OSX
