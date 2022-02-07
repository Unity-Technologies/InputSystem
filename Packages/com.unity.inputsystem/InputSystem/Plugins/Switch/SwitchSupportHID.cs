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
                    .WithCapability("vendorId", 0x057e) // Nintendo
                    .WithCapability("productId", 0x2009)); // Pro Controller.
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0f0d) // Hori Co., Ltd
                    .WithCapability("productId", 0x00c1)); // HORIPAD for Nintendo Switch
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x20d6) // PowerA NSW Fusion Wired FightPad
                    .WithCapability("productId", 0xa712));
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f) // PDP Wired Fight Pad Pro: Mario
                    .WithCapability("productId", 0x0185));
			InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0f0d ) // Hori Co., Ltd
                    .WithCapability("productId", 0x0092)); // Pokken Tournament DX Pro Pad
        #endif
        }
    }
}
#endif
