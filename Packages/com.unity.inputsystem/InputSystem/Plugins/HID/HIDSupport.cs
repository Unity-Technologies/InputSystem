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
        /// <summary>
        /// A pair of HID usage page and HID usage number.
        /// </summary>
        /// <remarks>
        /// Used to describe a HID usage for the <see cref="supportedHIDUsages"/> property.
        /// </remarks>
        public struct HIDPageUsage
        {
            /// <summary>
            /// The usage page.
            /// </summary>
            public HID.UsagePage page;

            /// <summary>
            /// A number specifying the usage on the usage page.
            /// </summary>
            public int usage;

            /// <summary>
            /// Create a HIDPageUsage struct by specifying a page and usage.
            /// </summary>
            public HIDPageUsage(HID.UsagePage page, int usage)
            {
                this.page = page;
                this.usage = usage;
            }

            /// <summary>
            /// Create a HIDPageUsage struct from the GenericDesktop usage page by specifying the usage.
            /// </summary>
            public HIDPageUsage(HID.GenericDesktop usage)
            {
                page = HID.UsagePage.GenericDesktop;
                this.usage = (int)usage;
            }
        }

        private static HIDPageUsage[] s_SupportedHIDUsages;

        /// <summary>
        /// An array of HID usages the input is configured to support.
        /// </summary>
        /// <remarks>
        /// The input system will only create <see cref="InputDevice"/>s for HIDs with usages
        /// listed in this array. Any other HID will be ignored. This saves the input system from
        /// spending resources on creating layouts and devices for HIDs which are not supported or
        /// not usable for game input.
        ///
        /// By default, this includes only <see cref="HID.GenericDesktop.Joystick"/>,
        /// <see cref="HID.GenericDesktop.Gamepad"/> and <see cref="HID.GenericDesktop.MultiAxisController"/>,
        /// but you can set this property to include any other HID usages.
        ///
        /// Note that currently on macOS, the only HID usages which can be enabled are
        /// <see cref="HID.GenericDesktop.Joystick"/>, <see cref="HID.GenericDesktop.Gamepad"/>,
        /// <see cref="HID.GenericDesktop.MultiAxisController"/>, <see cref="HID.GenericDesktop.TabletPCControls"/>,
        /// and <see cref="HID.GenericDesktop.AssistiveControl"/>.
        /// </remarks>
        public static ReadOnlyArray<HIDPageUsage> supportedHIDUsages
        {
            get => s_SupportedHIDUsages;
            set => s_SupportedHIDUsages = value.ToArray();
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
            s_SupportedHIDUsages = new[]
            {
                new HIDPageUsage(HID.GenericDesktop.Joystick),
                new HIDPageUsage(HID.GenericDesktop.Gamepad),
                new HIDPageUsage(HID.GenericDesktop.MultiAxisController),
            };

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
                        HIDDescriptorWindow.CreateOrShowExisting(device.deviceId, device.description);
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
