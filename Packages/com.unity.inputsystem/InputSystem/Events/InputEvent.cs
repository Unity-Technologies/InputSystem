using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;
using UnityEngineInternal.Input;

////REVIEW: can we get rid of the timestamp offsetting in the player and leave that complication for the editor only?

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A chunk of memory signaling a data transfer in the input system.
    /// </summary>
    /// <remarks>
    /// Input events are raw memory buffers akin to a byte array. For most uses of the input
    /// system, it is not necessary to be aware of the event stream in the background. Events
    /// are written to the internal event buffer by producers -- usually by the platform-specific
    /// backends sitting in the Unity runtime. Once per fixed or dynamic update (depending on
    /// what <see cref="InputSettings.updateMode"/> is set to), the input system then goes and
    /// flushes out the internal event buffer to process pending events.
    ///
    /// Events may signal general device-related occurrences (such as <see cref="DeviceConfigurationEvent"/>
    /// or <see cref="DeviceRemoveEvent"/>) or they may signal input activity. The latter kind of
    /// event is called "state events". In particular, these events are either <see cref="StateEvent"/>,
    /// only.
    ///
    /// Events are solely focused on input. To effect output on an input device (e.g. haptics
    /// effects), "commands" (see <see cref="InputDeviceCommand"/>) are used.
    ///
    /// Event processing can be listened to using <see cref="InputSystem.onEvent"/>. This callback
    /// will get triggered for each event as it is processed by the input system.
    ///
    /// Note that there is no "routing" mechanism for events, i.e. no mechanism by which the input
    /// system looks for a handler for a specific event. Instead, events represent low-level activity
    /// that the input system directly integrates into the state of its <see cref="InputDevice"/>
    /// instances.
    ///
    /// Each type of event is distinguished by its own <see cref="FourCC"/> type tag. The tag can
    /// be queried from the <see cref="type"/> property.
    ///
    /// Each event will receive a unique ID when queued to the internal event buffer. The ID can
    /// be queried using the <see cref="eventId"/> property. Over the lifetime of the input system,
    /// no two events will receive the same ID. If you repeatedly queue an event from the same
    /// memory buffer, each individual call of <see cref="InputSystem.QueueEvent"/> will result in
    /// its own unique event ID.
    ///
    /// All events are device-specific meaning that <see cref="deviceId"/> will always reference
    /// some device (which, however, may or may not translate to an <see cref="InputDevice"/>; that
    /// part depends on whether the input system was able to create an <see cref="InputDevice"/>
    /// based on the information received from the backend).
    ///
    /// To implement your own type of event, TODO (manual?)
    /// </remarks>
    /// <seealso cref="InputEventPtr"/>
    // NOTE: This has to be layout compatible with native events.
    [StructLayout(LayoutKind.Explicit, Size = kBaseEventSize, Pack = 1)]
    public struct InputEvent
    {
        private const uint kHandledMask = 0x80000000;
        private const uint kIdMask = 0x7FFFFFFF;

        internal const int kBaseEventSize = NativeInputEvent.structSize;

        /// <summary>
        /// Default, invalid value for <see cref="eventId"/>. Upon being queued with
        /// <see cref="InputSystem.QueueEvent"/>, no event will receive this ID.
        /// </summary>
        public const int InvalidEventId = 0;

        internal const int kAlignment = 4;

        [FieldOffset(0)]
        private NativeInputEvent m_Event;

        /// <summary>
        /// Type code for the event.
        /// </summary>
        /// <remarks>
        /// Each type of event has its own unique FourCC tag. For example, state events (see <see cref="StateEvent"/>)
        /// are tagged with "STAT". The type tag for a specific type of event can be queried from its <c>Type</c>
        /// property (for example, <see cref="StateEvent.Type"/>).
        ///
        /// To check whether an event has a specific type tag, you can use <see cref="InputEventPtr.IsA{T}"/>.
        /// </remarks>
        public FourCC type
        {
            get => new FourCC((int)m_Event.type);
            set => m_Event.type = (NativeInputEventType)(int)value;
        }

        /// <summary>
        /// Total size of the event in bytes.
        /// </summary>
        /// <value>Size of the event in bytes.</value>
        /// <remarks>
        /// Events are variable-size structs. This field denotes the total size of the event
        /// as stored in memory. This includes the full size of this struct and not just the
        /// "payload" of the event.
        ///
        /// <example>
        /// <code>
        /// // Store event in private buffer:
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
        ///
        /// The maximum supported size of events is <c>ushort.MaxValue</c>, i.e. events cannot
        /// be larger than 64KB.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="value"/> exceeds <c>ushort.MaxValue</c>.</exception>
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
        /// Events are assigned running IDs when they are put on an event queue (see
        /// <see cref="InputSystem.QueueEvent"/>).
        /// </remarks>
        /// <seealso cref="InvalidEventId"/>
        public int eventId
        {
            get => (int)(m_Event.eventId & kIdMask);
            set => m_Event.eventId = value | (int)(m_Event.eventId & ~kIdMask);
        }

        /// <summary>
        /// ID of the device that the event is for.
        /// </summary>
        /// <remarks>
        /// Device IDs are allocated by the <see cref="IInputRuntime">runtime</see>. No two devices
        /// will receive the same ID over an application lifecycle regardless of whether the devices
        /// existed at the same time or not.
        /// </remarks>
        /// <seealso cref="InputDevice.deviceId"/>
        /// <seealso cref="InputSystem.GetDeviceById"/>
        /// <seealso cref="InputDevice.InvalidDeviceId"/>
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
        ///
        /// Note that this implies that event times will reset in the editor every time you
        /// go into play mode. In effect, this can result in events appearing with negative
        /// timestamps (i.e. the event was generated before the current zero point for
        /// <see cref="Time.realtimeSinceStartup"/>).
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

        ////FIXME: this API isn't consistent; time seems to be internalTime whereas time property is external time
        public InputEvent(FourCC type, int sizeInBytes, int deviceId, double time = -1)
        {
            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;

            m_Event.type = (NativeInputEventType)(int)type;
            m_Event.sizeInBytes = (ushort)sizeInBytes;
            m_Event.deviceId = (ushort)deviceId;
            m_Event.time = time;
            m_Event.eventId = InvalidEventId;
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
            Debug.Assert(currentPtr != null, "Event pointer must not be NULL");
            var alignedSizeInBytes = currentPtr->sizeInBytes.AlignToMultipleOf(kAlignment);
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
            Debug.Assert(currentPtr != null, "Event pointer must not be NULL");

            var alignedSizeInBytes = currentPtr->sizeInBytes.AlignToMultipleOf(kAlignment);
            var nextPtr = (InputEvent*)((byte*)currentPtr + alignedSizeInBytes);

            if (!buffer.Contains(nextPtr))
                throw new InvalidOperationException(
                    $"Event '{new InputEventPtr(currentPtr)}' is last event in given buffer with size {buffer.sizeInBytes}");

            return nextPtr;
        }

        public static unsafe bool Equals(InputEvent* first, InputEvent* second)
        {
            if (first == second)
                return true;
            if (first == null || second == null)
                return false;

            if (first->m_Event.sizeInBytes != second->m_Event.sizeInBytes)
                return false;

            return UnsafeUtility.MemCmp(first, second, first->m_Event.sizeInBytes) == 0;
        }
    }
}
