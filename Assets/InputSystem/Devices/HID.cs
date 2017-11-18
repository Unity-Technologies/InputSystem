#if UNITY_STANDALONE || UNITY_EDITOR // ATM we only use HID on desktop.
using System;

namespace ISX
{
    // A generic HID device.
    //
    // This class represents a best effort to mirror the control setup of a HID
    // discovered in the system. It is used only as a fallback where we cannot
    // match the device to a specific product we know of. Wherever possible we
    // construct more specific device representations such as Gamepad.
    public class HID : InputDevice
    {
        public HIDDeviceDescriptor hidDescriptor => new HIDDeviceDescriptor();

        [Serializable]
        public struct HIDElementDescriptor
        {
        }

        [Serializable]
        public struct HIDDeviceDescriptor
        {
            public int usageID;
            public int usagePageID;
            public HIDElementDescriptor[] elements;
        }

        internal static void InitializeHIDSupport()
        {
            throw new NotImplementedException();
        }

        internal static void OnDeviceDiscovered(InputDeviceDescription description, string json, string matchedTemplate)
        {
            // If the system found a matching template, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedTemplate))
                return;

            // If the device isn't a HID, we're not interested.
            if (description.interfaceName != "HID")
                return;
        }
    }
}

#endif // UNITY_STANDALONE || UNITY_EDITOR
