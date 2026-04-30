#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.HID.Editor
{
    /// <summary>
    /// Handles Editor-specific initialization for HID support.
    /// </summary>
    [InitializeOnLoad]
    internal static class HIDSupportEditorInitializer
    {
        private static readonly GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");

        static HIDSupportEditorInitializer()
        {
            // Add toolbar button to any devices using the "HID" interface. Opens
            // a window to browse the HID descriptor of the device.
            InputDeviceDebuggerWindow.onToolbarGUI += OnDeviceToolbarGUI;
        }

        private static void OnDeviceToolbarGUI(InputDevice device)
        {
            if (device.description.interfaceName == HID.kHIDInterface)
            {
                if (GUILayout.Button(s_HIDDescriptor, EditorStyles.toolbarButton))
                {
                    HIDDescriptorWindow.CreateOrShowExisting(device.deviceId, device.description);
                }
            }
        }
    }
}
#endif
