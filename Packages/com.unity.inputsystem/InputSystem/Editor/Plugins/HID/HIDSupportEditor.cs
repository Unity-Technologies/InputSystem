using UnityEditor;
using UnityEngine.InputSystem.HID.Editor;

namespace UnityEngine.InputSystem.HID {

    internal static class HIDSupportEditor {
        private static readonly GUIContent s_HIDDescriptor = new GUIContent("HID Descriptor");

        internal static void InputDeviceDebuggerWindowToolbarGUI(InputDevice device) {
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