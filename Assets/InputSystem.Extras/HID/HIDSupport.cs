using UnityEngine;

namespace ISX.HID
{
    /// <summary>
    /// Adds support for generic HID devices to the input system.
    /// </summary>
    /// <remarks>
    /// Even without this module, HIDs can be used on platforms where we
    /// support HID has a native backend (Windows and OSX, at the moment).
    /// However, each support HID requires a template specifically targeting
    /// it as a product.
    ///
    /// What this module adds is the ability to turn any HID with usable
    /// controls into an InputDevice. It will make a best effort to figure
    /// out a suitable class for the device and will use the HID elements
    /// present in the HID descriptor to populate the device.
    /// </remarks>
    [InputPlugin(
         description = "Support for surfacing HIDs as input devices without knowing the specific products being used.",
         supportedPlatforms = new[]
    {
        RuntimePlatform.WindowsEditor,
        RuntimePlatform.WindowsPlayer,
        RuntimePlatform.OSXEditor,
        RuntimePlatform.OSXPlayer,
    })]
    public static class HIDSupport
    {
        /// <summary>
        /// Add support for generic HIDs to InputSystem.
        /// </summary>
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<HID>();
            InputSystem.onFindTemplateForDevice += HID.OnFindTemplateForDevice;
        }
    }
}
