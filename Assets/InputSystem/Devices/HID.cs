#if UNITY_STANDALONE || UNITY_EDITOR // ATM we only use HID on desktop.
using System;

namespace ISX
{
    internal static class HID
    {
        [Serializable]
        internal struct HIDElementDescriptor
        {
        }

        [Serializable]
        internal struct HIDDeviceDescriptor
        {
            public int usageID;
            public int usagePageID;
            public HIDElementDescriptor[] elements;
        }
    }
}

#endif // UNITY_STANDALONE || UNITY_EDITOR
