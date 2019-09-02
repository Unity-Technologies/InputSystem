using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.HID.Editor;
#endif

namespace UnityEngine.InputSystem.HID
{
    using ShouldCreateHIDCallback = System.Func<HID.HIDDeviceDescriptor, bool?>;

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
        public struct HIDPageUsage
        {
            public HID.UsagePage page;
            public int usage;

            public HIDPageUsage(HID.UsagePage page, int usage)
            {
                this.page = page;
                this.usage = usage;
            }

            public HIDPageUsage(HID.GenericDesktop usage)
            {
                this.page = HID.UsagePage.GenericDesktop;
                this.usage = (int)usage;
            }
        }


        public static ReadOnlyArray<HIDPageUsage> supportedHIDUsages { get; set; }

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
            supportedHIDUsages = new ReadOnlyArray<HIDPageUsage>(new[]
            {
                new HIDPageUsage(HID.GenericDesktop.Joystick),
                new HIDPageUsage(HID.GenericDesktop.Gamepad),
                new HIDPageUsage(HID.GenericDesktop.MultiAxisController),
            });

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
        private static readonly GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");
        #endif
    }
}
