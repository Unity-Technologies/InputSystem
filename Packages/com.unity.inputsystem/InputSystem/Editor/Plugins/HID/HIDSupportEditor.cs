using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.HID.Editor;

namespace UnityEngine.InputSystem.HID
{
    public static class HIDSupportEditor
    {
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
            // Add toolbar button to any devices using the "HID" interface. Opens
            // a windows to browse the HID descriptor of the device.
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
        }

        private static GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");
    }
}
