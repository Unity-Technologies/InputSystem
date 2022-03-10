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
                    .WithCapability("productId", 0x0092)); // Pokken Tournament DX Pro Pad
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0f0d) // Hori Co., Ltd
                    .WithCapability("productId", 0x00aa)); // Real Arcade Pro
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0f0d) // Hori Co., Ltd
                    .WithCapability("productId", 0x00c1)); // HORIPAD for Nintendo Switch
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0f0d) // Hori Co., Ltd
                    .WithCapability("productId", 0x00dc)); // Fighting Commander
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0f0d) // Hori Co., Ltd
                    .WithCapability("productId", 0x00f6)); // HORI Wireless Switch Pad
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f) // PDP
                    .WithCapability("productId", 0x0180)); // Faceoff Wired Pro Controller for Nintendo Switch
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f) // PDP
                    .WithCapability("productId", 0x0181)); // Faceoff Deluxe Wired Pro Controller for Nintendo Switch
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f) // PDP
                    .WithCapability("productId", 0x0185)); // Wired Fight Pad Pro
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f) // PDP
                    .WithCapability("productId", 0x0186)); //  Afterglow Wireless Switch Controller - "Nintento Wireless Gamepad"
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f) // PDP
                    .WithCapability("productId", 0x0187)); // Rock Candy Wired Controller for Nintendo Switch
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x20d6) // PowerA
                    .WithCapability("productId", 0xa712)); // NSW Fusion Wired FightPad
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x20d6) // PowerA
                    .WithCapability("productId", 0xa716)); // NSW Fusion Pro Controller

            // gamepads below currently break Mac Editor and Standalone
            #if !(UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f)     // PDP
                    .WithCapability("productId", 0x0184));     // Faceoff Premiere Wired Pro Controller for Nintendo Switch
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x0e6f)     // PDP
                    .WithCapability("productId", 0x0188));     // Afterglow Deluxe+ Audio Wired Controller
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x20d6)     // PowerA
                    .WithCapability("productId", 0xa714));     // NSW Spectra Wired Controller
            InputSystem.RegisterLayoutMatcher<SwitchProControllerHID>(
                new InputDeviceMatcher()
                    .WithInterface("HID")
                    .WithCapability("vendorId", 0x20d6)     // PowerA
                    .WithCapability("productId", 0xa715));     // Fusion Wireless Arcade Stick
            #endif
        #endif
        }
    }
}
#endif
