using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using ISX.Plugins.HID.Editor;
#endif

namespace ISX.Plugins.HID
{
    /// <summary>
    /// Adds support for generic HID devices to the input system.
    /// </summary>
    /// <remarks>
    /// Even without this module, HIDs can be used on platforms where we
    /// support HID has a native backend (Windows and OSX, at the moment).
    /// However, each supported HID requires a template specifically targeting
    /// it as a product.
    ///
    /// What this module adds is the ability to turn any HID with usable
    /// controls into an InputDevice. It will make a best effort to figure
    /// out a suitable class for the device and will use the HID elements
    /// present in the HID report descriptor to populate the device.
    /// </remarks>
    [InputPlugin]
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

#if UNITY_EDITOR
        public static void OnToolbarGUI(InputDevice device)
        {
            var hid = device as HID;
            if (hid == null)
                return;

            if (GUILayout.Button(s_HIDDescriptor, EditorStyles.toolbarButton))
            {
                HIDDescriptorWindow.CreateOrShowExisting(hid);
            }
        }

        private static GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");
#endif
    }
}
