using UnityEngine;

namespace ISX.HID
{
    [InputPlugin]
    public static class HIDSupport
    {
        public static string description =>
        "Support for surfacing HIDs as input devices without knowing the specific products being used.";

        public static RuntimePlatform[] supportedPlatforms =>
        new[]
        {
            RuntimePlatform.WindowsEditor,
            RuntimePlatform.WindowsPlayer,
            RuntimePlatform.OSXEditor,
            RuntimePlatform.OSXPlayer,
            RuntimePlatform.LinuxEditor,
            RuntimePlatform.LinuxPlayer
        };

        public static void Initialize()
        {
            InputSystem.RegisterTemplate<HID>();
            InputSystem.onDeviceDiscovered += HID.OnDeviceDiscovered;
        }
    }
}
