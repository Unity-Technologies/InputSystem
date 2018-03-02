using UnityEngine;

namespace ISX.PS4DualShock
{
    [InputPlugin(description = "Support for PlayStation DualShock gamepads on PS4.",
         supportedPlatforms = new[]
    {
        RuntimePlatform.WindowsEditor,
        RuntimePlatform.PS4,
    })]
    public static class PS4DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<PS4TouchControl>("PS4Touch");

            InputSystem.RegisterTemplate<PS4DualShockGamepad>();
        }
    }
}
