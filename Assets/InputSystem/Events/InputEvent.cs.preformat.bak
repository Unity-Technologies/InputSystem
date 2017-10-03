using System.Runtime.InteropServices;

namespace ISX
{
    // A chunk of memory signaling a data transfer in the input system.
    // This has to be layout compatible with native events.
    [StructLayout(LayoutKind.Sequential)]
    public struct InputEvent : IInputEventTypeInfo
    {
        private FourCC m_Type;
        private int m_SizeInBytes;
        private int m_DeviceId;
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

        public FourCC GetTypeStatic()
        {
            return new FourCC(); // No valid type code; InputEvent is considered abstract.
        }
        public int GetSizeStatic()
        {
            return 0;
        }
        
        public InputEvent(FourCC type, int sizeInBytes, int deviceId, double time)
        {
            m_Type = type;
            m_SizeInBytes = sizeInBytes;
            m_DeviceId = deviceId;
            m_Time = time;
        }
    }
}