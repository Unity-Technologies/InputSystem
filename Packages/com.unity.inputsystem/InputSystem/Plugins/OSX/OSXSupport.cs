#if UNITY_EDITOR || UNITY_STANDALONE_OSX
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OSX.LowLevel;

namespace UnityEngine.InputSystem.OSX
{
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class OSXSupport
    {
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
