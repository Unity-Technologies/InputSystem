using System;

namespace ISX.HID
{
    /// <summary>
    /// Turns binary HID descriptors into <see cref="HID.HIDDeviceDescriptor"/> instances.
    /// </summary>
    public static class HIDParser
    {
        public static HID.HIDDeviceDescriptor Parse(IntPtr buffer, int size)
        {
            throw new NotImplementedException();
        }
    }
}
