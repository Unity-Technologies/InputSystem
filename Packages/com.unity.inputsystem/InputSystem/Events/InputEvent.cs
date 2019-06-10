using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;
using UnityEngineInternal.Input;

////REVIEW: can we get rid of the timestamp offsetting in the player and leave that complication for the editor only?
#if !UNITY_2019_2
// NativeInputEventType/NativeInputEvent are marked obsolete in 19.1, because they are becoming internal in 19.2
#pragma warning disable 618
#endif

namespace UnityEngine.InputSystem.LowLevel
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

        internal const int kBaseEventSize = 20;
        public const int InvalidId = 0;
        internal const int kAlignment = 4;

        [FieldOffset(0)]
        private NativeInputEvent m_Event;

        /// <summary>
        /// Type code for the event.
        /// </summary>
        public FourCC type
        {
            get => new FourCC((int)m_Event.type);
            set => m_Event.type = (NativeInputEventType)(int)value;
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
            get => m_Event.sizeInBytes;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentException("Maximum event size is " + ushort.MaxValue, nameof(value));
                m_Event.sizeInBytes = (ushort)value;
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
            get => (int)(m_Event.eventId & kIdMask);
            set => m_Event.eventId = (int)(value | (int)(m_Event.eventId & ~kIdMask));
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
            get => m_Event.deviceId;
            set => m_Event.deviceId = (ushort)value;
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
            get => m_Event.time - InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
            set => m_Event.time = value + InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;
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
            get => m_Event.time;
            set => m_Event.time = value;
        }

        static InputEvent()
        {
            unsafe
            {
                Debug.Assert(kBaseEventSize == sizeof(NativeInputEvent), "kBaseEventSize sizemust match NativeInputEvent struct size.");
            }
        }

        public InputEvent(FourCC type, int sizeInBytes, int deviceId, double time = -1)
        {
            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;

            m_Event.type = (NativeInputEventType)(int)type;
            m_Event.sizeInBytes = (ushort)sizeInBytes;
            m_Event.deviceId = (ushort)deviceId;
            m_Event.time = time;
            m_Event.eventId = InvalidId;
        }

        // We internally use bits inside m_EventId as flags. IDs are linearly counted up by the
        // native input system starting at 1 so we have plenty room.
        // NOTE: The native system assigns IDs when events are queued so if our handled flag
        //       will implicitly get overwritten. Having events go back to unhandled state
        //       when they go on the queue makes sense in itself, though, so this is fine.
        public bool handled
        {
            get => (m_Event.eventId & kHandledMask) == kHandledMask;
            set
            {
                if (value)
                    m_Event.eventId = (int)(m_Event.eventId | kHandledMask);
                else
                    m_Event.eventId = (int)(m_Event.eventId & ~kHandledMask);
            }
        }

        public override string ToString()
        {
            return $"id={eventId} type={type} device={deviceId} size={sizeInBytes} time={time}";
        }

        /// <summary>
        /// Get the next event after the given one.
        /// </summary>
        /// <param name="currentPtr">A valid event pointer.</param>
        /// <returns>Pointer to the next event in memory.</returns>
        /// <remarks>
        /// This method applies no checks and must only be called if there is an event following the
        /// given one. Also, the size of the given event must be 100% as the method will simply
        /// take the size and advance the given pointer by it (and aligning it to <see cref="kAlignment"/>).
        /// </remarks>
        /// <seealso cref="GetNextInMemoryChecked"/>
        internal static unsafe InputEvent* GetNextInMemory(InputEvent* currentPtr)
        {
            Debug.Assert(currentPtr != null);
            var alignedSizeInBytes = NumberHelpers.AlignToMultiple(currentPtr->sizeInBytes, kAlignment);
            return (InputEvent*)((byte*)currentPtr + alignedSizeInBytes);
        }

        /// <summary>
        /// Get the next event after the given one. Throw if that would point to invalid memory as indicated
        /// by the given memory buffer.
        /// </summary>
        /// <param name="currentPtr">A valid event pointer to an event inside <paramref name="buffer"/>.</param>
        /// <param name="buffer">Event buffer in which to advance to the next event.</param>
        /// <returns>Pointer to the next event.</returns>
        /// <exception cref="InvalidOperationException">There are no more events in the given buffer.</exception>
        internal static unsafe InputEvent* GetNextInMemoryChecked(InputEvent* currentPtr, ref InputEventBuffer buffer)
        {
            Debug.Assert(currentPtr != null);
            Debug.Assert(buffer.Contains(currentPtr), "Given event is not contained in given event buffer");

            var alignedSizeInBytes = NumberHelpers.AlignToMultiple(currentPtr->sizeInBytes, kAlignment);
            var nextPtr = (InputEvent*)((byte*)currentPtr + alignedSizeInBytes);

            if (!buffer.Contains(nextPtr))
                throw new InvalidOperationException(
                    $"Event '{new InputEventPtr(currentPtr)}' is last event in given buffer with size {buffer.sizeInBytes}");

            return nextPtr;
        }
    }
}
