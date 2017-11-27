using System;
using System.Runtime.InteropServices;

namespace ISX
{
    /// <summary>
    /// An InputEventBuffer that can be concurrently written to and read from in FIFO order.
    /// </summary>
    /// <remarks>
    /// Event queues have a fixed size. If events are produced faster than they are consumed,
    /// new events will start overwriting old events.
    ///
    /// Event size is also fixed. While smaller events can be written to the queue, larger
    /// events will be rejected.
    ///
    /// Memory for event queues is allocated on the native heap and queues can be written to
    /// and read from both in C++ and in C#.
    /// </remarks>
    public struct InputEventQueue
    {
        public InputEventQueue(int maxEventSizeInBytes, int maxEventCount)
        {
        }

        internal InputEventQueue(IntPtr nativeQueuePtr)
        {
        }

        public void WriteEvent(InputEventPtr eventPtr)
        {
            throw new NotImplementedException();
        }

        public InputEventPtr TryReadNextEvent()
        {
            throw new NotImplementedException();
        }

        // Data layout of queue in native memory.
        // The queue consists of a header, a variable-size table of event pointers, and
        // a variable-size buffer of event storage.
        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct QueueData
        {
            [FieldOffset(0)] public uint maxEventSizeInBytes;
            [FieldOffset(4)] public uint maxEventCount;
        }
    }
}
