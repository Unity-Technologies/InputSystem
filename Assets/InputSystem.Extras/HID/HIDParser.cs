using System;
using System.IO;

namespace ISX.HID
{
    /// <summary>
    /// Turns binary HID descriptors into <see cref="HID.HIDDeviceDescriptor"/> instances.
    /// </summary>
    /// <remarks>
    /// For information about the format, see the <see cref="http://www.usb.org/developers/hidpage/HID1_11.pdf">
    /// Device Class Definition for Human Interface Devices</see> section 6.2.2.
    /// </remarks>
    public static class HIDParser
    {
        public enum HIDItemType
        {
            Input,
            Output,
            Feature,
            Collection,
            EndCollection
        }

        public struct HIDItem
        {
            public ushort? usage;
            public ushort? usagePage;
            public int? reportSize;
            public int? reportCount;
            public HIDItem[] children;

            public void Read(BinaryReader reader)
            {
                var firstByte = reader.ReadByte();

                if (firstByte == 0xfe)
                    throw new NotImplementedException("long item support");

                var bSize = (byte)(firstByte & 0x3);
                var bType = (byte)((firstByte & 0xC) >> 2);
                var bTag = (byte)((firstByte & 0xf0) >> 4);
            }

            public void Write(BinaryWriter writer)
            {
            }

            public byte[] ToArray()
            {
                throw new NotImplementedException();
            }
        }

        public static bool ParseReportDescriptor(byte[] buffer, ref HID.HIDDeviceDescriptor result)
        {
            throw new NotImplementedException();
        }
    }
}
