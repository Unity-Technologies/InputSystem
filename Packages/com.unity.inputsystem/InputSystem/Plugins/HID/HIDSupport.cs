using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Plugins.HID.Editor;
#endif
using UnityEngine.InputSystem.Utilities;
using ShouldCreateHIDCallback = System.Func<UnityEngine.InputSystem.Plugins.HID.HID.HIDDeviceDescriptor, bool?>;

namespace UnityEngine.InputSystem.Plugins.HID
{
    /// <summary>
    /// Adds support for generic HID devices to the input system.
    /// </summary>
    /// <remarks>
    /// Even without this module, HIDs can be used on platforms where we
    /// support HID has a native backend (Windows and OSX, at the moment).
    /// However, each supported HID requires a layout specifically targeting
    /// it as a product.
    ///
    /// What this module adds is the ability to turn any HID with usable
    /// controls into an InputDevice. It will make a best effort to figure
    /// out a suitable class for the device and will use the HID elements
    /// present in the HID report descriptor to populate the device.
    ///
    /// If there is an existing product-specific layout for a HID, it will
    /// take precedence and HIDSupport will leave the device alone.
    /// </remarks>
    public static class HIDSupport
    {
        public static event ShouldCreateHIDCallback shouldCreateHID
        {
            add => s_ShouldCreateHID.Append(value);
            remove => s_ShouldCreateHID.Remove(value);
        }

        internal static InlinedArray<ShouldCreateHIDCallback> s_ShouldCreateHID;

        private static bool? DefaultShouldCreateHIDCallback(HID.HIDDeviceDescriptor descriptor)
        {
            if (descriptor.usagePage == HID.UsagePage.GenericDesktop)
            {
                switch (descriptor.usage)
                {
                    case (int)HID.GenericDesktop.Joystick:
                    case (int)HID.GenericDesktop.Gamepad:
                    case (int)HID.GenericDesktop.MultiAxisController:
                        return true;
                }
            }
            return null;
        }

        /// <summary>
        /// Add support for generic HIDs to InputSystem.
        /// </summary>
#if UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
        public
#else
        internal
#endif
        static void Initialize()
        {
            s_ShouldCreateHID.Append(DefaultShouldCreateHIDCallback);

            InputSystem.RegisterLayout<HID>();
            InputSystem.onFindLayoutForDevice += HID.OnFindLayoutForDevice;

            // Add toolbar button to any devices using the "HID" interface. Opens
            // a windows to browse the HID descriptor of the device.
            #if UNITY_EDITOR
            InputDeviceDebuggerWindow.onToolbarGUI +=
                device =>
            {
                if (device.description.interfaceName == HID.kHIDInterface)
                {
                    if (GUILayout.Button(s_HIDDescriptor, EditorStyles.toolbarButton))
                    {
                        HIDDescriptorWindow.CreateOrShowExisting(device.id, device.description);
                    }
                }
            };
            #endif
        }

        #if UNITY_EDITOR
        private static GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");
        #endif
    }
}
