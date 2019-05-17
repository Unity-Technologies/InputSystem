////TODO: add support for Windows.Gaming.Input.Gamepad (including the trigger motors)

using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.XInput
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

#if UNITY_EDITOR || UNITY_XBOXONE
            InputSystem.RegisterLayout<XboxOneGamepad>(
                matches: new InputDeviceMatcher()
                    .WithDeviceClass("XboxOneGamepad")
                    .WithInterface("Xbox"));
#endif
            ////FIXME: layouts should always be available in the editor (mac/win/linux)
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
            InputSystem.RegisterLayout<XInputControllerWindows>(
                matches: new InputDeviceMatcher().WithInterface("XInput"));
#endif
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            InputSystem.RegisterLayout<XInputControllerOSX>(
                matches: new InputDeviceMatcher().WithInterface("HID")
                    .WithProduct("Xbox.*Wired Controller"));
            InputSystem.RegisterLayout<XInputControllerWirelessOSX>(
                matches: new InputDeviceMatcher().WithInterface("HID")
                    .WithProduct("Xbox.*Wireless Controller"));
#endif
        }
    }
}
