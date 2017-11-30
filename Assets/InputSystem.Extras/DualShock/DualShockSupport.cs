using UnityEngine;

namespace ISX.DualShock
{
    [InputPlugin(description = "Support for PlayStation DualShock gamepads.",
         supportedPlatforms = new[]
    {
        RuntimePlatform.WindowsEditor,
        RuntimePlatform.WindowsPlayer,
        RuntimePlatform.OSXEditor,
        RuntimePlatform.OSXPlayer,
        RuntimePlatform.LinuxEditor,
        RuntimePlatform.LinuxPlayer,
        RuntimePlatform.PS4,
    })]
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<DualShockGamepad>();
        }
    }
}
