////TODO: add support for Windows.Gaming.Input.Gamepad (including the trigger motors)

using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XInput
{
    /// <summary>
    /// Adds support for XInput controllers.
    /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
    public
#else
    internal
#endif
    static class XInputSupport
    {
        public static void Initialize()
        {
            // Base layout for Xbox-style gamepad.
            InputSystem.RegisterLayout<XInputController>();

            ////FIXME: layouts should always be available in the editor (mac/win/linux)
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
            InputSystem.RegisterLayout<XInputControllerWindows>(
                matches: new InputDeviceMatcher().WithInterface("XInput"));
#endif
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // Legacy support when a user is using the 360Controller driver on macOS <= 10.15
            InputSystem.RegisterLayout<XboxGamepadMacOS>(
                matches: new InputDeviceMatcher().WithInterface("HID")
                    .WithProduct("Xbox.*Wired Controller"));


            // Matches macOS native support for Xbox Controllers
            // macOS reports all Xbox controllers as "Controller" with manufacter Microsoft
            InputSystem.RegisterLayout<XboxGamepadMacOSNative>(
                matches: new InputDeviceMatcher().WithInterface("HID")
                    .WithProduct("Controller").WithManufacturer("Microsoft"));

            // Matching older Xbox One controllers that have different View and Share buttons than the newer Xbox Series
            // controllers.
            // Reported inhttps://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1264
            // Based on devices from this list
            // https://github.com/mdqinc/SDL_GameControllerDB/blob/a453871de2e0e2484544514c6c080e1e916d620c/gamecontrollerdb.txt#L798C1-L806C1
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02B0);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02D1);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02DD);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02E0);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02E3);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02EA);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02FD);
            RegisterXboxOneWirelessFromProductAndVendorID(0x045E, 0x02FF);

            // This layout is for all the other Xbox One or Series controllers that have the same View and Share buttons.
            // Reported in https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-385
            InputSystem.RegisterLayout<XboxGamepadMacOSWireless>(
                matches: new InputDeviceMatcher().WithInterface("HID")
                    .WithProduct("Xbox.*Wireless Controller"));

            void RegisterXboxOneWirelessFromProductAndVendorID(int vendorId, int productId)
            {
                InputSystem.RegisterLayout<XboxOneGampadMacOSWireless>(
                    matches: new InputDeviceMatcher().WithInterface("HID")
                        .WithProduct("Xbox.*Wireless Controller")
                        .WithCapability("vendorId", vendorId)
                        .WithCapability("productId", productId));
            }

#endif
        }
    }
}
