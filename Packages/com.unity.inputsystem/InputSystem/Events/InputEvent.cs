using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: can we get rid of the timestamp offsetting in the player and leave that complication for the editor only?

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A chunk of memory signaling a data transfer in the input system.
    /// </summary>
    // NOTE: This has to be layout compatible with native events.
    [StructLayout(LayoutKind.Explicit, Size = kBaseEventSize)]
    public struct InputEvent
    {
        private const uint kHandledMask = 0x80000000;
        private const uint kIdMask = 0x7FFFFFFF;

        public const int kBaseEventSize = 20;
        public const int kInvalidId = 0;
        public const int kAlignment = 4;

        [FieldOffset(0)] private FourCC m_Type;
        [FieldOffset(4)] private ushort m_SizeInBytes;
        [FieldOffset(6)] private ushort m_DeviceId;
        [FieldOffset(8)] internal uint m_EventId;
        [FieldOffset(12)] private double m_Time;

        /// <summary>
        /// Type code for the event.
        /// </summary>
        public FourCC type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        /// <summary>
        /// Total size of the event in bytes.
        /// </summary>
        /// <remarks>
        /// Events are variable-size structs. This field denotes the total size of the event
        /// as stored in memory. This includes the full size of this struct and not just the
        /// "payload" of the event.
        /// </remarks>
        /// <example>
        /// Store event in private buffer:
        /// <code>
        /// unsafe byte[] CopyEventData(InputEventPtr eventPtr)
        /// {
        ///     var sizeInBytes = eventPtr.sizeInBytes;
        ///     var buffer = new byte[sizeInBytes];
        ///     fixed (byte* bufferPtr = buffer)
        ///     {
        ///         UnsafeUtility.MemCpy(new IntPtr(bufferPtr), eventPtr.data, sizeInBytes);
        ///     }
        ///     return buffer;
        /// }
        /// </code>
        /// </example>
        public uint sizeInBytes
        {
            get { return m_SizeInBytes; }
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentException("Maximum event size is " + ushort.MaxValue, "value");
                m_SizeInBytes = (ushort)value;
            }
        }

        /// <summary>
        /// Unique serial ID of the event.
        /// </summary>
        /// <remarks>
        /// Events are assigned running IDs when they are put on an event queue.
        /// </remarks>
        public int eventId
        {
            get { return (int)(m_EventId & kIdMask); }
            set { m_EventId = (uint)value | (m_EventId & ~kIdMask); }
        }

        /// <summary>
        /// ID of the device that the event is for.
        /// </summary>
        /// <remarks>
        /// Device IDs are allocated by the <see cref="IInputRuntime">runtime</see>. No two devices
        /// will receive the same ID over an application lifecycle regardless of whether the devices
        /// existed at the same time or not.
        /// </remarks>
        /// <seealso cref="InputDevice.id"/>
        /// <seealso cref="InputSystem.GetDeviceById"/>
        public int deviceId
        {
            get { return m_DeviceId; }
            set { m_DeviceId = (ushort)value; }
        }

        /// <summary>
        /// Time that the event was generated at.
        /// </summary>
        /// <remarks>
        /// Times are in seconds and progress linearly in real-time. The timeline is the
        /// same as for <see cref="Time.realtimeSinceStartup"/>.
        /// </remarks>
        public double time
        {
            get { return m_Time - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup; }
            set { m_Time = value + InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup; }
        }

        /// <summary>
        /// This is the raw input timestamp without the offset to <see cref="Time.realtimeSinceStartup"/>.
        /// </summary>
        /// <remarks>
        /// Internally, we always store all timestamps in "input time" which is relative to the native
        /// function GetTimeSinceStartup(). <see cref="IInputRuntime.currentTime"/> yields the current
        /// time on this timeline.
        /// </remarks>
        internal double internalTime
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        public InputEvent(FourCC type, int sizeInBytes, int deviceId, double time)
        {
            m_Type = type;
            m_SizeInBytes = (ushort)sizeInBytes;
            m_DeviceId = (ushort)deviceId;
            m_Time = time;
            m_EventId = kInvalidId;
        }

        // We internally use bits inside m_EventId as flags. IDs are linearly counted up by the
        // native input system starting at 1 so we have plenty room.
        // NOTE: The native system assigns IDs when events are queued so if our handled flag
        //       will implicitly get overwritten. Having events go back to unhandled state
        //       when they go on the queue makes sense in itself, though, so this is fine.
        public bool handled
        {
            get { return (m_EventId & kHandledMask) == kHandledMask; }
            set
            {
                if (value)
                    m_EventId |= kHandledMask;
                else
                    m_EventId &= ~kHandledMask;
            }
        }

        public override string ToString()
        {
            return string.Format("id={0} type={1} device={2} size={3} time={4}",
                eventId, type, deviceId, sizeInBytes, time);
        }

        internal static unsafe InputEvent* GetNextInMemory(InputEvent* current)
        {
            var alignedSizeInBytes = NumberHelpers.AlignToMultiple(current->sizeInBytes, kAlignment);
            return (InputEvent*)((byte*)current + alignedSizeInBytes);
        }
    }
}
