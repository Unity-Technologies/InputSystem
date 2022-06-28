using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Notifies about the insertion of an input device
    /// (alternative to SendDeviceDiscoveriesToScript() directly sending NotifyDeviceDiscovered)
    /// </summary>
    /// <remarks>
    /// Device that got connected is the one identified by <see cref="InputEvent.deviceId"/>
    /// of <see cref="baseEvent"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + 4 + 1)]
    public unsafe struct DeviceInsertEvent : IInputEventTypeInfo
    {
        public const int Type = 0x44494E53; // DINS

        /// <summary>
        /// Common event data.
        /// </summary>
        [FieldOffset(0)] public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize + 0)] public int descriptorLength;
        [FieldOffset(InputEvent.kBaseEventSize + 4)] public fixed byte descriptorText[1]; // Variable-sized.

        public FourCC typeStatic => Type;

        public string descriptor
        {
            get
            {
                fixed (byte* data = descriptorText)
                {
                    return System.Text.Encoding.ASCII.GetString(data, descriptorLength);
                }
            }
        }

        public InputEventPtr ToEventPtr()
        {
            fixed (DeviceInsertEvent* ptr = &this)
            {
                return new InputEventPtr((InputEvent*)ptr);
            }
        }

        public static DeviceInsertEvent Create(int deviceId, string descriptorText, double time)
        {
            var inputEvent =
                new DeviceInsertEvent { baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + 4 + descriptorText.Length, deviceId, time) };
            inputEvent.descriptorLength = descriptorText.Length;
            byte[] srcByteArray = System.Text.Encoding.ASCII.GetBytes(descriptorText);
            fixed(byte* srcBytes = srcByteArray)
            {
                byte* dstBytes = inputEvent.descriptorText;
                Marshal.Copy(srcByteArray, 0, (System.IntPtr)dstBytes, descriptorText.Length);
            }

            return inputEvent;
        }
    }
}
