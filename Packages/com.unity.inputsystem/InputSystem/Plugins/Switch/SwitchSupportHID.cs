#if UNITY_EDITOR || UNITY_SWITCH || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA
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
    static class SwitchSupportHID
    {
        public static void Initialize()
        {
        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            InputSystem.RegisterLayout<SwitchProControllerHID>(
                matches: new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x57e) // Nintendo
                    .WithCapability("productId", 0x2009)); // Pro Controller.
        #endif
        }
    }
}
#endif
