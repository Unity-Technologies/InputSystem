using UnityEngine;

namespace ISX.XboxOne
{
    [InputPlugin(description = "Support for gamepads on XboxOne.",
         supportedPlatforms = new[]
    {
        RuntimePlatform.WindowsEditor,
        RuntimePlatform.XboxOne,
    })]
    public static class XboxOneGamepadSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<XboxOneGamepad>();
        }
    }
}
