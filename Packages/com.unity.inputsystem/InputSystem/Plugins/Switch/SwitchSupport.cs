#if UNITY_EDITOR || UNITY_SWITCH || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Switch
{
    /// <summary>
    /// Adds support for Switch NPad controllers.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class SwitchSupport
    {
        public static void Initialize()
        {
        #if UNITY_EDITOR || UNITY_SWITCH
            InputSystem.RegisterLayout<NPadSwitch>(
                matches: new InputDeviceMatcher()
                    .WithInterface("Switch")
                    .WithManufacturer("Nintendo")
                    .WithProduct("Wireless Controller"));
        #endif
        #if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
            InputSystem.RegisterLayout<NPadHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x57e) // Nintendo
                    .WithCapability("productId", 0x2009)); // Pro Controller.
        #endif
        }
    }
}
#endif // UNITY_EDITOR || UNITY_SWITCH
