using System.Runtime.InteropServices;

namespace ISX
{
    // A chunk of memory signaling a data transfer in the input system.
    // This has to be layout compatible with native events.
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct InputEvent : IInputEventTypeInfo
    {
        [FieldOffset(0)]
        private FourCC m_Type;
        [FieldOffset(4)]
        private int m_SizeInBytes;
        [FieldOffset(8)]
        private int m_DeviceId;
        [FieldOffset(12)]
        private double m_Time;

        public FourCC type { get { return m_Type; } }
        public int sizeInBytes { get { return m_SizeInBytes; } }

        public int deviceId
        {
            get { return m_DeviceId; }
            set { m_DeviceId = value; }
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
            m_DeviceId = deviceId;
            m_Time = time;
        }

        public FourCC GetTypeStatic()
        {
            return new FourCC(); // No valid type code; InputEvent is considered abstract.
        }

        public int GetSizeStatic()
        {
            return 0;
        }

        // We internally use bits inside m_DeviceId as flags. Device IDs are
        // linearly counted up by the native input system starting at 1 so we
        // have plenty room in m_DeviceId.
        internal bool handled
        {
            get { return (m_DeviceId & 0x8000000) != 0; }
            set { m_DeviceId |= 0x8000000; }
        }
    }
}
