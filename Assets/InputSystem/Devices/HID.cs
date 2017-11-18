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
        public const string kHIDInterface = "HID";

        // The HID device descriptor as received from the device driver.
        public HIDDeviceDescriptor hidDescriptor => new HIDDeviceDescriptor();

        // NOTE: Must match HIDReportType in native.
        [Serializable]
        public enum HIDReportType
        {
            Input,
            Output,
            Feature
        }

        // NOTE: Must match up with the serialization represention of HIDInputElementDescriptor in native.
        [Serializable]
        public struct HIDElementDescriptor
        {
            public string name;
            public int usageId;
            public int usagePageId;
            public int unit;
            public int unitExponent;
            public int logicalMin;
            public int logicalMax;
            public int physicalMin;
            public int physicalMax;
            public HIDReportType reportType;
            public int reportID;
            public int reportCount;
            public int reportSizeInBits;
            public bool hasNullState;
            public bool hasPreferredState;
            public bool isArray;
            public bool isNonLinear;
            public bool isRelative;
            public bool isVirtual;
            public bool isWrapping;
        }

        // NOTE: Must match up with the serialized representation of HIDInputDeviceDescriptor in native.
        [Serializable]
        public struct HIDDeviceDescriptor
        {
            public int vendorID;
            public int productID;
            public int usageID;
            public int usagePageID;
            public HIDElementDescriptor[] elements;
        }

        internal static void InitializeHIDSupport(InputManager manager)
        {
            throw new NotImplementedException();
        }

        internal static string OnDeviceDiscovered(InputDeviceDescription description, string matchedTemplate)
        {
            // If the system found a matching template, there's nothing for us to do.
            if (!string.IsNullOrEmpty(matchedTemplate))
                return null;

            // If the device isn't a HID, we're not interested.
            if (description.interfaceName != kHIDInterface)
                return null;

            return null;
        }
    }
}

#endif // UNITY_STANDALONE || UNITY_EDITOR
