using System.Runtime.InteropServices;

namespace ISX
{
    // A chunk of memory signaling a data transfer in the input system.
    // This has to be layout compatible with native events.
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize)]
    public struct InputEvent : IInputEventTypeInfo
    {
        private const uint kHandledMask = 0x80000000;
        private const uint kDeviceIdMask = 0x7FFFFFFF;

        public const int kBaseEventSize = 20;

        [FieldOffset(0)]
        private FourCC m_Type;
        [FieldOffset(4)]
        private int m_SizeInBytes;
        [FieldOffset(8)]
        private uint m_DeviceId;
        [FieldOffset(12)]
        private double m_Time;

        public FourCC type => m_Type;
        public int sizeInBytes => m_SizeInBytes;

        public int deviceId
        {
            // Need to mask out handled bit.
            get { return (int)(m_DeviceId & kDeviceIdMask); }
            set { m_DeviceId = (m_DeviceId & kHandledMask) | (uint)value; }
        }

        public double time
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        public InputEvent(FourCC type, int sizeInBytes, int deviceId, double time)
        {
            m_Type = type;
            m_SizeInBytes = sizeInBytes;
            m_DeviceId = (uint)deviceId;
            m_Time = time;
        }

        public FourCC GetTypeStatic()
        {
            return new FourCC(); // No valid type code; InputEvent is considered abstract.
        }

        // We internally use bits inside m_DeviceId as flags. Device IDs are
        // linearly counted up by the native input system starting at 1 so we
        // have plenty room in m_DeviceId.
        public bool handled
        {
            get { return (m_DeviceId & kHandledMask) == kHandledMask; }
            set { m_DeviceId |= kHandledMask; }
        }

        public override string ToString()
        {
            return $"type = {type}, device = {deviceId}, size = {sizeInBytes}, time = {time}";
        }
    }
}
