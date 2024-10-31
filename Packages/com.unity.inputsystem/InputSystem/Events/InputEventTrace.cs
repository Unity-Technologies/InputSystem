using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.InputSystem.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using Unity.Profiling;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// InputEventTrace lets you record input events for later processing. It also has features for writing traces
    /// to disk, for loading them from disk, and for playing back previously recorded traces.
    /// </summary>
    /// <remarks>
    /// InputEventTrace lets you record input events into a buffer for either a specific device, or for all events
    /// received by the input system. This is useful for testing purposes or for replaying recorded input.
    ///
    /// Note that event traces <em>must</em> be disposed of (by calling <see cref="Dispose"/>) after use or they
    /// will leak memory on the unmanaged (C++) memory heap.
    ///
    /// Event traces are serializable such that they can survive domain reloads in the editor.
    /// </remarks>
    [Serializable]
    public sealed unsafe class InputEventTrace : IDisposable, IEnumerable<InputEventPtr>
    {
        private const int kDefaultBufferSize = 1024 * 1024;
        private static readonly ProfilerMarker k_InputEvenTraceMarker = new ProfilerMarker("InputEventTrace");

        /// <summary>
        /// If <see name="recordFrameMarkers"/> is enabled, an <see cref="InputEvent"/> with this <see cref="FourCC"/>
        /// code in its <see cref="InputEvent.type"/> is recorded whenever the input system starts a new update, i.e.
        /// whenever <see cref="InputSystem.onBeforeUpdate"/> is triggered. This is useful for replaying events in such
        /// a way that they are correctly spaced out over frames.
        /// </summary>
        public static FourCC FrameMarkerEvent => new FourCC('F', 'R', 'M', 'E');

        /// <summary>
        /// Set device to record events for. Set to <see cref="InputDevice.InvalidDeviceId"/> by default
        /// in which case events from all devices are recorded.
        /// </summary>
        public int deviceId
        {
            get => m_DeviceId;
            set => m_DeviceId = value;
        }

        /// <summary>
        /// Whether the trace is currently recording input.
        /// </summary>
        /// <value>True if the trace is currently recording events.</value>
        /// <seealso cref="Enable"/>
        /// <seealso cref="Disable"/>
        public bool enabled => m_Enabled;

        /// <summary>
        /// If true, input update boundaries will be recorded as events. By default, this is off.
        /// </summary>
        /// <value>Whether frame boundaries should be recorded in the trace.</value>
        /// <remarks>
        /// When recording with this off, all events are written one after the other for as long
        /// as the recording is active. This means that when a recording runs over multiple frames,
        /// it is no longer possible for the trace to tell which events happened in distinct frames.
        ///
        /// By turning this feature on, frame marker events (i.e. <see cref="InputEvent"/> instances
        /// with <see cref="InputEvent.type"/> set to <see cref="FrameMarkerEvent"/>) will be written
        /// to the trace every time an input update occurs. When playing such a trace back via <see
        /// cref="ReplayController.PlayAllFramesOneByOne"/>, events will get spaced out over frames corresponding
        /// to how they were spaced out when input was initially recorded.
        ///
        /// Note that having this feature enabled will fill up traces much quicker. Instead of being
        /// filled up only when there is input, TODO
        /// </remarks>
        /// <seealso cref="ReplayController.PlayAllFramesOneByOne"/>
        /// <seealso cref="FrameMarkerEvent"/>
        public bool recordFrameMarkers
        {
            get => m_RecordFrameMarkers;
            set
            {
                if (m_RecordFrameMarkers == value)
                    return;
                m_RecordFrameMarkers = value;
                if (m_Enabled)
                {
                    if (value)
                        InputSystem.onBeforeUpdate += OnBeforeUpdate;
                    else
                        InputSystem.onBeforeUpdate -= OnBeforeUpdate;
                }
            }
        }

        /// <summary>
        /// Total number of events currently in the trace.
        /// </summary>
        /// <value>Number of events recorded in the trace.</value>
        public long eventCount => m_EventCount;

        /// <summary>
        /// The amount of memory consumed by all events combined that are currently
        /// stored in the trace.
        /// </summary>
        /// <value>Total size of event data currently in trace.</value>
        public long totalEventSizeInBytes => m_EventSizeInBytes;

        /// <summary>
        /// Total size of memory buffer (in bytes) currently allocated.
        /// </summary>
        /// <value>Size of memory currently allocated.</value>
        /// <remarks>
        /// The buffer is allocated on the unmanaged heap.
        /// </remarks>
        public long allocatedSizeInBytes => m_EventBuffer != default ? m_EventBufferSize : 0;

        /// <summary>
        /// Largest size (in bytes) that the memory buffer is allowed to grow to. By default, this is
        /// the same as <see cref="allocatedSizeInBytes"/> meaning that the buffer is not allowed to grow but will
        /// rather wrap around when full.
        /// </summary>
        /// <value>Largest size the memory buffer is allowed to grow to.</value>
        public long maxSizeInBytes => m_MaxEventBufferSize;

        /// <summary>
        /// Information about all devices for which events have been recorded in the trace.
        /// </summary>
        /// <value>Record of devices recorded in the trace.</value>
        public ReadOnlyArray<DeviceInfo> deviceInfos => m_DeviceInfos;

        /// <summary>
        /// Optional delegate to decide whether an input should be stored in a trace. Null by default.
        /// </summary>
        /// <value>Delegate to accept or reject individual events.</value>
        /// <remarks>
        /// When this is set, the callback will be invoked on every event that would otherwise be stored
        /// directly in the trace. If the callback returns <c>true</c>, the trace will continue to record
        /// the event. If the callback returns <c>false</c>, the event will be ignored and not recorded.
        ///
        /// The callback should generally mutate the event. If you do so, note that this will impact
        /// event processing in general, not just recording of the event in the trace.
        /// </remarks>
        public Func<InputEventPtr, InputDevice, bool> onFilterEvent
        {
            get => m_OnFilterEvent;
            set => m_OnFilterEvent = value;
        }

        /// <summary>
        /// Event that is triggered every time an event has been recorded in the trace.
        /// </summary>
        public event Action<InputEventPtr> onEvent
        {
            add => m_EventListeners.AddCallback(value);
            remove => m_EventListeners.RemoveCallback(value);
        }

        public InputEventTrace(InputDevice device, long bufferSizeInBytes = kDefaultBufferSize, bool growBuffer = false,
                               long maxBufferSizeInBytes = -1, long growIncrementSizeInBytes = -1)
            : this(bufferSizeInBytes, growBuffer, maxBufferSizeInBytes, growIncrementSizeInBytes)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            m_DeviceId = device.deviceId;
        }

        /// <summary>
        /// Create a disabled event trace that does not perform any allocation yet. An event trace only starts consuming resources
        /// the first time it is enabled.
        /// </summary>
        /// <param name="bufferSizeInBytes">Size of buffer that will be allocated on first event captured by trace. Defaults to 1MB.</param>
        /// <param name="growBuffer">If true, the event buffer will be grown automatically when it reaches capacity, up to a maximum
        /// size of <paramref name="maxBufferSizeInBytes"/>. This is off by default.</param>
        /// <param name="maxBufferSizeInBytes">If <paramref name="growBuffer"/> is true, this is the maximum size that the buffer should
        /// be grown to. If the maximum size is reached, old events are being overwritten.</param>
        public InputEventTrace(long bufferSizeInBytes = kDefaultBufferSize, bool growBuffer = false, long maxBufferSizeInBytes = -1, long growIncrementSizeInBytes = -1)
        {
            m_EventBufferSize = (uint)bufferSizeInBytes;

            if (growBuffer)
            {
                if (maxBufferSizeInBytes < 0)
                    m_MaxEventBufferSize = 256 * kDefaultBufferSize;
                else
                    m_MaxEventBufferSize = maxBufferSizeInBytes;

                if (growIncrementSizeInBytes < 0)
                    m_GrowIncrementSize = kDefaultBufferSize;
                else
                    m_GrowIncrementSize = growIncrementSizeInBytes;
            }
            else
            {
                m_MaxEventBufferSize = m_EventBufferSize;
            }
        }

        /// <summary>
        /// Write the contents of the event trace to a file.
        /// </summary>
        /// <param name="filePath">Path of the file to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c> or empty.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> is invalid.</exception>
        /// <exception cref="DirectoryNotFoundException">A directory in <paramref name="filePath"/> is invalid.</exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="filePath"/> cannot be accessed.</exception>
        /// <seealso cref="ReadFrom(string)"/>
        public void WriteTo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var stream = File.OpenWrite(filePath))
                WriteTo(stream);
        }

        /// <summary>
        /// Write the contents of the event trace to the given stream.
        /// </summary>
        /// <param name="stream">Stream to write the data to. Must support seeking (i.e. <c>Stream.canSeek</c> must be true).</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support seeking.</exception>
        /// <exception cref="IOException">An error occurred trying to write to <paramref name="stream"/>.</exception>
        public void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("Stream does not support seeking", nameof(stream));

            var writer = new BinaryWriter(stream);

            var flags = default(FileFlags);
            if (InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate)
                flags |= FileFlags.FixedUpdate;

            // Write header.
            writer.Write(kFileFormat);
            writer.Write(kFileVersion);
            writer.Write((int)flags);
            writer.Write((int)Application.platform);
            writer.Write((ulong)m_EventCount);
            writer.Write((ulong)m_EventSizeInBytes);

            // Write events.
            foreach (var eventPtr in this)
            {
                ////TODO: find way to directly write a byte* buffer to the stream instead of copying to a temp byte[]

                var sizeInBytes = eventPtr.sizeInBytes;
                var buffer = new byte[sizeInBytes];
                fixed(byte* bufferPtr = buffer)
                {
                    UnsafeUtility.MemCpy(bufferPtr, eventPtr.data, sizeInBytes);
                    writer.Write(buffer);
                }
            }

            // Write devices.
            writer.Flush();
            var positionOfDeviceList = stream.Position;
            var deviceCount = m_DeviceInfos.LengthSafe();
            writer.Write(deviceCount);
            for (var i = 0; i < deviceCount; ++i)
            {
                ref var device = ref m_DeviceInfos[i];
                writer.Write(device.deviceId);
                writer.Write(device.layout);
                writer.Write(device.stateFormat);
                writer.Write(device.stateSizeInBytes);
                writer.Write(device.m_FullLayoutJson ?? string.Empty);
            }

            // Write offset of device list.
            writer.Flush();
            var offsetOfDeviceList = stream.Position - positionOfDeviceList;
            writer.Write(offsetOfDeviceList);
        }

        /// <summary>
        /// Read the contents of an input event trace stored in the given file.
        /// </summary>
        /// <param name="filePath">Path to a file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c> or empty.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> is invalid.</exception>
        /// <exception cref="DirectoryNotFoundException">A directory in <paramref name="filePath"/> is invalid.</exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="filePath"/> cannot be accessed.</exception>
        /// <remarks>
        /// This method replaces the contents of the trace with those read from the given file.
        /// </remarks>
        /// <seealso cref="WriteTo(string)"/>
        public void ReadFrom(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var stream = File.OpenRead(filePath))
                ReadFrom(stream);
        }

        /// <summary>
        /// Read the contents of an input event trace from the given stream.
        /// </summary>
        /// <param name="stream">A stream of binary data containing a recorded event trace as written out with <see cref="WriteTo(Stream)"/>.
        /// Must support reading.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support reading.</exception>
        /// <exception cref="IOException">An error occurred trying to read from <paramref name="stream"/>.</exception>
        /// <remarks>
        /// This method replaces the contents of the event trace with those read from the stream. It does not append
        /// to the existing trace.
        /// </remarks>
        /// <seealso cref="WriteTo(Stream)"/>
        public void ReadFrom(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream does not support reading", nameof(stream));

            var reader = new BinaryReader(stream);

            // Read header.
            if (reader.ReadInt32() != kFileFormat)
                throw new IOException($"Stream does not appear to be an InputEventTrace (no '{kFileFormat}' code)");
            if (reader.ReadInt32() > kFileVersion)
                throw new IOException($"Stream is an InputEventTrace but a newer version (expected version {kFileVersion} or below)");
            reader.ReadInt32(); // Flags; ignored for now.
            reader.ReadInt32(); // Platform; for now we're not doing anything with it.
            var eventCount = reader.ReadUInt64();
            var totalEventSizeInBytes = reader.ReadUInt64();
            var oldBuffer = m_EventBuffer;

            if (eventCount > 0 && totalEventSizeInBytes > 0)
            {
                // Allocate buffer, if need be.
                byte* buffer;
                if (m_EventBuffer != null && m_EventBufferSize >= (long)totalEventSizeInBytes)
                {
                    // Existing buffer is large enough.
                    buffer = m_EventBuffer;
                }
                else
                {
                    buffer = (byte*)UnsafeUtility.Malloc((long)totalEventSizeInBytes, InputEvent.kAlignment, Allocator.Persistent);
                    m_EventBufferSize = (long)totalEventSizeInBytes;
                }
                try
                {
                    // Read events.
                    var tailPtr = buffer;
                    var endPtr = tailPtr + totalEventSizeInBytes;
                    var totalEventSize = 0L;
                    for (var i = 0ul; i < eventCount; ++i)
                    {
                        var eventType = reader.ReadInt32();
                        var eventSizeInBytes = (uint)reader.ReadUInt16();
                        var eventDeviceId = (uint)reader.ReadUInt16();

                        if (eventSizeInBytes > endPtr - tailPtr)
                            break;

                        *(int*)tailPtr = eventType;
                        tailPtr += 4;
                        *(ushort*)tailPtr = (ushort)eventSizeInBytes;
                        tailPtr += 2;
                        *(ushort*)tailPtr = (ushort)eventDeviceId;
                        tailPtr += 2;

                        ////TODO: find way to directly read from stream into a byte* pointer
                        var remainingSize = (int)eventSizeInBytes - sizeof(int) - sizeof(short) - sizeof(short);
                        var tempBuffer = reader.ReadBytes(remainingSize);
                        fixed(byte* tempBufferPtr = tempBuffer)
                        UnsafeUtility.MemCpy(tailPtr, tempBufferPtr, remainingSize);

                        tailPtr += remainingSize.AlignToMultipleOf(InputEvent.kAlignment);
                        totalEventSize += eventSizeInBytes.AlignToMultipleOf(InputEvent.kAlignment);

                        if (tailPtr >= endPtr)
                            break;
                    }

                    // Read device infos.
                    var deviceCount = reader.ReadInt32();
                    var deviceInfos = new DeviceInfo[deviceCount];
                    for (var i = 0; i < deviceCount; ++i)
                    {
                        deviceInfos[i] = new DeviceInfo
                        {
                            deviceId = reader.ReadInt32(),
                            layout = reader.ReadString(),
                            stateFormat = reader.ReadInt32(),
                            stateSizeInBytes = reader.ReadInt32(),
                            m_FullLayoutJson = reader.ReadString()
                        };
                    }

                    // Install buffer.
                    m_EventBuffer = buffer;
                    m_EventBufferHead = m_EventBuffer;
                    m_EventBufferTail = endPtr;
                    m_EventCount = (long)eventCount;
                    m_EventSizeInBytes = totalEventSize;
                    m_DeviceInfos = deviceInfos;
                }
                catch
                {
                    if (buffer != oldBuffer)
                        UnsafeUtility.Free(buffer, Allocator.Persistent);
                    throw;
                }
            }
            else
            {
                m_EventBuffer = default;
                m_EventBufferHead = default;
                m_EventBufferTail = default;
            }

            // Release old buffer, if we've switched to a new one.
            if (m_EventBuffer != oldBuffer && oldBuffer != null)
                UnsafeUtility.Free(oldBuffer, Allocator.Persistent);

            ++m_ChangeCounter;
        }

        /// <summary>
        /// Load an input event trace from the given file.
        /// </summary>
        /// <param name="filePath">Path to a file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <c>null</c> or empty.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> is invalid.</exception>
        /// <exception cref="DirectoryNotFoundException">A directory in <paramref name="filePath"/> is invalid.</exception>
        /// <exception cref="UnauthorizedAccessException"><paramref name="filePath"/> cannot be accessed.</exception>
        /// <seealso cref="WriteTo(string)"/>
        /// <seealso cref="ReadFrom(string)"/>
        public static InputEventTrace LoadFrom(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            using (var stream = File.OpenRead(filePath))
                return LoadFrom(stream);
        }

        /// <summary>
        /// Load an event trace from a previously captured event stream.
        /// </summary>
        /// <param name="stream">A stream as written by <see cref="WriteTo(Stream)"/>. Must support reading.</param>
        /// <returns>The loaded event trace.</returns>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="IOException">The stream cannot be loaded (e.g. wrong format; details in the exception).</exception>
        /// <seealso cref="WriteTo(Stream)"/>
        public static InputEventTrace LoadFrom(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable", nameof(stream));

            var trace = new InputEventTrace();
            trace.ReadFrom(stream);

            return trace;
        }

        /// <summary>
        /// Start a replay of the events in the trace.
        /// </summary>
        /// <returns>An object that controls playback.</returns>
        /// <remarks>
        /// Calling this method implicitly turns off recording, if currently enabled (i.e. it calls <see cref="Disable"/>),
        /// as replaying an event trace cannot be done while it is also concurrently modified.
        /// </remarks>
        public ReplayController Replay()
        {
            Disable();
            return new ReplayController(this);
        }

        /// <summary>
        /// Resize the current event memory buffer to the specified size.
        /// </summary>
        /// <param name="newBufferSize">Size to allocate for the buffer.</param>
        /// <param name="newMaxBufferSize">Optional parameter to specifying the mark up to which the buffer is allowed to grow. By default,
        /// this is negative which indicates the buffer should not grow. In this case, <see cref="maxSizeInBytes"/> will be set
        /// to <paramref name="newBufferSize"/>. If this parameter is a non-negative number, it must be greater than or equal to
        /// <paramref name="newBufferSize"/> and will become the new value for <see cref="maxSizeInBytes"/>.</param>
        /// <returns>True if the new buffer was successfully allocated.</returns>
        /// <exception cref="ArgumentException"><paramref name="newBufferSize"/> is negative.</exception>
        public bool Resize(long newBufferSize, long newMaxBufferSize = -1)
        {
            if (newBufferSize <= 0)
                throw new ArgumentException("Size must be positive", nameof(newBufferSize));

            if (m_EventBufferSize == newBufferSize)
                return true;

            if (newMaxBufferSize < newBufferSize)
                newMaxBufferSize = newBufferSize;

            // Allocate.
            var newEventBuffer = (byte*)UnsafeUtility.Malloc(newBufferSize, InputEvent.kAlignment, Allocator.Persistent);
            if (newEventBuffer == default)
                return false;

            // If we have existing contents, migrate them.
            if (m_EventCount > 0)
            {
                // If we're shrinking the buffer or have a buffer that has already wrapped around,
                // migrate events one by one.
                if (newBufferSize < m_EventBufferSize || m_HasWrapped)
                {
                    var fromPtr = new InputEventPtr((InputEvent*)m_EventBufferHead);
                    var toPtr = (InputEvent*)newEventBuffer;
                    var newEventCount = 0;
                    var newEventSizeInBytes = 0;
                    var remainingEventBytes = m_EventSizeInBytes;

                    for (var i = 0; i < m_EventCount; ++i)
                    {
                        var eventSizeInBytes = fromPtr.sizeInBytes;
                        var alignedEventSizeInBytes = eventSizeInBytes.AlignToMultipleOf(InputEvent.kAlignment);

                        // We only start copying once we know that the remaining events we have fit in the new buffer.
                        // This way we get the newest events and not the oldest ones.
                        if (remainingEventBytes <= newBufferSize)
                        {
                            UnsafeUtility.MemCpy(toPtr, fromPtr.ToPointer(), eventSizeInBytes);
                            toPtr = InputEvent.GetNextInMemory(toPtr);
                            newEventSizeInBytes += (int)alignedEventSizeInBytes;
                            ++newEventCount;
                        }

                        remainingEventBytes -= alignedEventSizeInBytes;
                        if (!GetNextEvent(ref fromPtr))
                            break;
                    }

                    m_HasWrapped = false;
                    m_EventCount = newEventCount;
                    m_EventSizeInBytes = newEventSizeInBytes;
                }
                else
                {
                    // Simple case of just having to copy everything between head and tail.
                    UnsafeUtility.MemCpy(newEventBuffer,
                        m_EventBufferHead,
                        m_EventSizeInBytes);
                }
            }

            if (m_EventBuffer != null)
                UnsafeUtility.Free(m_EventBuffer, Allocator.Persistent);

            m_EventBufferSize = newBufferSize;
            m_EventBuffer = newEventBuffer;
            m_EventBufferHead = newEventBuffer;
            m_EventBufferTail = m_EventBuffer + m_EventSizeInBytes;
            m_MaxEventBufferSize = newMaxBufferSize;

            ++m_ChangeCounter;

            return true;
        }

        /// <summary>
        /// Reset the trace. Clears all recorded events.
        /// </summary>
        public void Clear()
        {
            m_EventBufferHead = m_EventBufferTail = default;
            m_EventCount = 0;
            m_EventSizeInBytes = 0;
            ++m_ChangeCounter;
            m_DeviceInfos = null;
        }

        /// <summary>
        /// Start recording events.
        /// </summary>
        /// <seealso cref="Disable"/>
        public void Enable()
        {
            if (m_Enabled)
                return;

            if (m_EventBuffer == default)
                Allocate();

            InputSystem.onEvent += OnInputEvent;
            if (m_RecordFrameMarkers)
                InputSystem.onBeforeUpdate += OnBeforeUpdate;

            m_Enabled = true;
        }

        /// <summary>
        /// Stop recording events.
        /// </summary>
        /// <seealso cref="Enable"/>
        public void Disable()
        {
            if (!m_Enabled)
                return;

            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onBeforeUpdate -= OnBeforeUpdate;

            m_Enabled = false;
        }

        /// <summary>
        /// Based on the given event pointer, return a pointer to the next event in the trace.
        /// </summary>
        /// <param name="current">A pointer to an event in the trace or a <c>default(InputEventTrace)</c>. In the former case,
        /// the pointer will be updated to the next event, if there is one. In the latter case, the pointer will be updated
        /// to the first event in the trace, if there is one.</param>
        /// <returns>True if <c>current</c> has been set to the next event, false otherwise.</returns>
        /// <remarks>
        /// Event storage in memory may be circular if the event buffer is fixed in size or has reached maximum
        /// size and new events start overwriting old events. This method will automatically start with the first
        /// event when the given <paramref name="current"/> event is null. Any subsequent call with then loop over
        /// the remaining events until no more events are available.
        ///
        /// Note that it is VERY IMPORTANT that the buffer is not modified while iterating over events this way.
        /// If this is not ensured, invalid memory accesses may result.
        ///
        /// <example>
        /// <code>
        /// // Loop over all events in the InputEventTrace in the `trace` variable.
        /// var current = default(InputEventPtr);
        /// while (trace.GetNextEvent(ref current))
        /// {
        ///     Debug.Log(current);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public bool GetNextEvent(ref InputEventPtr current)
        {
            if (m_EventBuffer == default)
                return false;

            // If head is null, tail is too and it means there's nothing in the
            // buffer yet.
            if (m_EventBufferHead == default)
                return false;

            // If current is null, start iterating at head.
            if (!current.valid)
            {
                current = new InputEventPtr((InputEvent*)m_EventBufferHead);
                return true;
            }

            // Otherwise feel our way forward.

            var nextEvent = (byte*)current.Next().data;
            var endOfBuffer = m_EventBuffer + m_EventBufferSize;

            // If we've run into our tail, there's no more events.
            if (nextEvent == m_EventBufferTail)
                return false;

            // If we've reached blank space at the end of the buffer, wrap
            // around to the beginning. In this scenario there must be an event
            // at the beginning of the buffer; tail won't position itself at
            // m_EventBuffer.
            if (endOfBuffer - nextEvent < InputEvent.kBaseEventSize ||
                ((InputEvent*)nextEvent)->sizeInBytes == 0)
            {
                nextEvent = m_EventBuffer;
                if (nextEvent == current.ToPointer())
                    return false; // There's only a single event in the buffer.
            }

            // We're good. There's still space between us and our tail.
            current = new InputEventPtr((InputEvent*)nextEvent);
            return true;
        }

        public IEnumerator<InputEventPtr> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Stop recording, if necessary, and clear the trace such that it released unmanaged
        /// memory which might be allocated.
        /// </summary>
        /// <remarks>
        /// For any trace that has recorded events, calling this method is crucial in order to not
        /// leak memory on the unmanaged (C++) memory heap.
        /// </remarks>
        public void Dispose()
        {
            Disable();
            Release();
        }

        // We want to make sure that it's not possible to iterate with an enumerable over
        // a trace that is being changed so we bump this counter every time we modify the
        // buffer and check in the enumerator that the counts match.
        [NonSerialized] private int m_ChangeCounter;
        [NonSerialized] private bool m_Enabled;
        [NonSerialized] private Func<InputEventPtr, InputDevice, bool> m_OnFilterEvent;

        [SerializeField] private int m_DeviceId = InputDevice.InvalidDeviceId;
        [NonSerialized] private CallbackArray<Action<InputEventPtr>> m_EventListeners;

        // Buffer for storing event trace. Allocated in native so that we can survive a
        // domain reload without losing event traces.
        // NOTE: Ideally this would simply use InputEventBuffer but we can't serialize that one because
        //       of the NativeArray it has inside. Also, due to the wrap-around nature, storage of
        //       events in the buffer may not be linear.
        [SerializeField] private long m_EventBufferSize;
        [SerializeField] private long m_MaxEventBufferSize;
        [SerializeField] private long m_GrowIncrementSize;
        [SerializeField] private long m_EventCount;
        [SerializeField] private long m_EventSizeInBytes;
        // These are ulongs for the sake of Unity serialization which can't handle pointers or IntPtrs.
        [SerializeField] private ulong m_EventBufferStorage;
        [SerializeField] private ulong m_EventBufferHeadStorage;
        [SerializeField] private ulong m_EventBufferTailStorage;
        [SerializeField] private bool m_HasWrapped;
        [SerializeField] private bool m_RecordFrameMarkers;
        [SerializeField] private DeviceInfo[] m_DeviceInfos;

        private byte* m_EventBuffer
        {
            get => (byte*)m_EventBufferStorage;
            set => m_EventBufferStorage = (ulong)value;
        }

        private byte* m_EventBufferHead
        {
            get => (byte*)m_EventBufferHeadStorage;
            set => m_EventBufferHeadStorage = (ulong)value;
        }

        private byte* m_EventBufferTail
        {
            get => (byte*)m_EventBufferTailStorage;
            set => m_EventBufferTailStorage = (ulong)value;
        }

        private void Allocate()
        {
            m_EventBuffer = (byte*)UnsafeUtility.Malloc(m_EventBufferSize, InputEvent.kAlignment, Allocator.Persistent);
        }

        private void Release()
        {
            Clear();

            if (m_EventBuffer != default)
            {
                UnsafeUtility.Free(m_EventBuffer, Allocator.Persistent);
                m_EventBuffer = default;
            }
        }

        private void OnBeforeUpdate()
        {
            ////TODO: make this work correctly with the different update types

            if (m_RecordFrameMarkers)
            {
                // Record frame marker event.
                // NOTE: ATM these events don't get valid event IDs. Might be this is even useful but is more a side-effect
                //       of there not being a method to obtain an ID except by actually queuing an event.
                var frameMarkerEvent = new InputEvent
                {
                    type = FrameMarkerEvent,
                    internalTime = InputRuntime.s_Instance.currentTime,
                    sizeInBytes = (uint)UnsafeUtility.SizeOf<InputEvent>()
                };

                OnInputEvent(new InputEventPtr((InputEvent*)UnsafeUtility.AddressOf(ref frameMarkerEvent)), null);
            }
        }

        private void OnInputEvent(InputEventPtr inputEvent, InputDevice device)
        {
            // Ignore events that are already marked as handled.
            if (inputEvent.handled)
                return;

            // Ignore if the event isn't for our device (except if it's a frame marker).
            if (m_DeviceId != InputDevice.InvalidDeviceId && inputEvent.deviceId != m_DeviceId && inputEvent.type != FrameMarkerEvent)
                return;

            // Give callback a chance to filter event.
            if (m_OnFilterEvent != null && !m_OnFilterEvent(inputEvent, device))
                return;

            // This shouldn't happen but ignore the event if we're not tracing.
            if (m_EventBuffer == default)
                return;

            var bytesNeeded = inputEvent.sizeInBytes.AlignToMultipleOf(InputEvent.kAlignment);

            // Make sure we can fit the event at all.
            if (bytesNeeded > m_MaxEventBufferSize)
                return;

            k_InputEvenTraceMarker.Begin();

            if (m_EventBufferTail == default)
            {
                // First event in buffer.
                m_EventBufferHead = m_EventBuffer;
                m_EventBufferTail = m_EventBuffer;
            }

            var newTail = m_EventBufferTail + bytesNeeded;
            var newTailOvertakesHead = newTail > m_EventBufferHead && m_EventBufferHead != m_EventBuffer;

            // If tail goes out of bounds, enlarge the buffer or wrap around to the beginning.
            var newTailGoesPastEndOfBuffer = newTail > m_EventBuffer + m_EventBufferSize;
            if (newTailGoesPastEndOfBuffer)
            {
                // If we haven't reached the max size yet, grow the buffer.
                if (m_EventBufferSize < m_MaxEventBufferSize && !m_HasWrapped)
                {
                    var increment = Math.Max(m_GrowIncrementSize, bytesNeeded.AlignToMultipleOf(InputEvent.kAlignment));
                    var newBufferSize = m_EventBufferSize + increment;
                    if (newBufferSize > m_MaxEventBufferSize)
                        newBufferSize = m_MaxEventBufferSize;

                    if (newBufferSize < bytesNeeded)
                    {
                        k_InputEvenTraceMarker.End();
                        return;
                    }

                    Resize(newBufferSize);

                    newTail = m_EventBufferTail + bytesNeeded;
                }

                // See if we fit.
                var spaceLeft = m_EventBufferSize - (m_EventBufferTail - m_EventBuffer);
                if (spaceLeft < bytesNeeded)
                {
                    // No, so wrap around.
                    m_HasWrapped = true;

                    // Make sure head isn't trying to advance into gap we may be leaving at the end of the
                    // buffer by wiping the space if it could fit an event.
                    if (spaceLeft >= InputEvent.kBaseEventSize)
                        UnsafeUtility.MemClear(m_EventBufferTail, InputEvent.kBaseEventSize);

                    m_EventBufferTail = m_EventBuffer;
                    newTail = m_EventBuffer + bytesNeeded;

                    // If the tail overtook both the head and the end of the buffer,
                    // we need to make sure the head is wrapped around as well.
                    if (newTailOvertakesHead)
                        m_EventBufferHead = m_EventBuffer;

                    // Recheck whether we're overtaking head.
                    newTailOvertakesHead = newTail > m_EventBufferHead;
                }
            }

            // If the new tail runs into head, bump head as many times as we need to
            // make room for the event. Head may itself wrap around here.
            if (newTailOvertakesHead)
            {
                var newHead = m_EventBufferHead;
                var endOfBufferMinusOneEvent =
                    m_EventBuffer + m_EventBufferSize - InputEvent.kBaseEventSize;

                while (newHead < newTail)
                {
                    var numBytes = ((InputEvent*)newHead)->sizeInBytes;
                    newHead += numBytes;
                    --m_EventCount;
                    m_EventSizeInBytes -= numBytes;
                    if (newHead > endOfBufferMinusOneEvent || ((InputEvent*)newHead)->sizeInBytes == 0)
                    {
                        newHead = m_EventBuffer;
                        break;
                    }
                }

                m_EventBufferHead = newHead;
            }

            var buffer = m_EventBufferTail;
            m_EventBufferTail = newTail;

            // Copy data to buffer.
            UnsafeUtility.MemCpy(buffer, inputEvent.data, inputEvent.sizeInBytes);
            ++m_ChangeCounter;
            ++m_EventCount;
            m_EventSizeInBytes += bytesNeeded;

            // Make sure we have a record for the device.
            if (device != null)
            {
                var haveRecord = false;
                if (m_DeviceInfos != null)
                    for (var i = 0; i < m_DeviceInfos.Length; ++i)
                        if (m_DeviceInfos[i].deviceId == device.deviceId)
                        {
                            haveRecord = true;
                            break;
                        }
                if (!haveRecord)
                    ArrayHelpers.Append(ref m_DeviceInfos, new DeviceInfo
                    {
                        m_DeviceId = device.deviceId,
                        m_Layout = device.layout,
                        m_StateFormat = device.stateBlock.format,
                        m_StateSizeInBytes = (int)device.stateBlock.alignedSizeInBytes,

                        // If it's a generated layout, store the full layout JSON in the device info. We do this so that
                        // when saving traces for this kind of input, we can recreate the device.
                        m_FullLayoutJson = InputControlLayout.s_Layouts.IsGeneratedLayout(device.m_Layout)
                            ? InputSystem.LoadLayout(device.layout).ToJson()
                            : null
                    });
            }

            // Notify listeners.
            if (m_EventListeners.length > 0)
                DelegateHelpers.InvokeCallbacksSafe(ref m_EventListeners, new InputEventPtr((InputEvent*)buffer),
                    "InputEventTrace.onEvent");

            k_InputEvenTraceMarker.End();
        }

        private class Enumerator : IEnumerator<InputEventPtr>
        {
            private InputEventTrace m_Trace;
            private int m_ChangeCounter;
            internal InputEventPtr m_Current;

            public Enumerator(InputEventTrace trace)
            {
                m_Trace = trace;
                m_ChangeCounter = trace.m_ChangeCounter;
            }

            public void Dispose()
            {
                m_Trace = null;
                m_Current = new InputEventPtr();
            }

            public bool MoveNext()
            {
                if (m_Trace == null)
                    throw new ObjectDisposedException(ToString());
                if (m_Trace.m_ChangeCounter != m_ChangeCounter)
                    throw new InvalidOperationException("Trace has been modified while enumerating!");

                return m_Trace.GetNextEvent(ref m_Current);
            }

            public void Reset()
            {
                m_Current = default;
                m_ChangeCounter = m_Trace.m_ChangeCounter;
            }

            public InputEventPtr Current => m_Current;
            object IEnumerator.Current => Current;
        }

        private static FourCC kFileFormat => new FourCC('I', 'E', 'V', 'T');
        private static int kFileVersion = 1;

        [Flags]
        private enum FileFlags
        {
            FixedUpdate = 1 << 0, // Events were recorded with system being in fixed-update mode.
        }

        /// <summary>
        /// Controls replaying of events recorded in an <see cref="InputEventTrace"/>.
        /// </summary>
        /// <remarks>
        /// Playback can be controlled either on a per-event or a per-frame basis. Note that playing back events
        /// frame by frame requires frame markers to be present in the trace (see <see cref="recordFrameMarkers"/>).
        ///
        /// By default, events will be queued as is except for their timestamps which will be set to the current
        /// time that each event is queued at.
        ///
        /// What this means is that events replay with the same device ID (see <see cref="InputEvent.deviceId"/>)
        /// they were captured on. If the trace is replayed in the same session that it was recorded in, this means
        /// that the events will replay on the same device (if it still exists).
        ///
        /// To map recorded events to a different device, you can either call <see cref="WithDeviceMappedFromTo(int,int)"/> to
        /// map an arbitrary device ID to a new one or call <see cref="WithAllDevicesMappedToNewInstances"/> to create
        /// new (temporary) devices for the duration of playback.
        ///
        /// <example>
        /// <code>
        /// var trace = new InputEventTrace(myDevice);
        /// trace.Enable();
        ///
        /// // ... run one or more frames ...
        ///
        /// trace.Replay().OneFrame();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventTrace.Replay"/>
        public class ReplayController : IDisposable
        {
            /// <summary>
            /// The event trace associated with the replay controller.
            /// </summary>
            /// <value>Trace from which events are replayed.</value>
            public InputEventTrace trace => m_EventTrace;

            /// <summary>
            /// Whether replay has finished.
            /// </summary>
            /// <value>True if replay has finished or is not in progress.</value>
            /// <seealso cref="PlayAllFramesOneByOne"/>
            /// <seealso cref="PlayAllEvents"/>
            public bool finished { get; private set; }

            /// <summary>
            /// Whether replay is paused.
            /// </summary>
            /// <value>True if replay is currently paused.</value>
            public bool paused { get; set; }

            /// <summary>
            /// Current position in the event stream.
            /// </summary>
            /// <value>Index of current event in trace.</value>
            public int position { get; private set; }

            /// <summary>
            /// List of devices created by the replay controller.
            /// </summary>
            /// <value>Devices created by the replay controller.</value>
            /// <remarks>
            /// By default, a replay controller will queue events as is, i.e. with <see cref="InputEvent.deviceId"/> of
            /// each event left as is. This means that the events will target existing devices (if any) that have the
            /// respective ID.
            ///
            /// Using <see cref="WithAllDevicesMappedToNewInstances"/>, a replay controller can be instructed to create
            /// new, temporary devices instead for each unique <see cref="InputEvent.deviceId"/> encountered in the stream.
            /// All devices created by the controller this way will be put on this list.
            /// </remarks>
            /// <seealso cref="WithAllDevicesMappedToNewInstances"/>
            public IEnumerable<InputDevice> createdDevices => m_CreatedDevices;

            private InputEventTrace m_EventTrace;
            private Enumerator m_Enumerator;
            private InlinedArray<KeyValuePair<int, int>> m_DeviceIDMappings;
            private bool m_CreateNewDevices;
            private InlinedArray<InputDevice> m_CreatedDevices;
            private Action m_OnFinished;
            private Action<InputEventPtr> m_OnEvent;
            private double m_StartTimeAsPerFirstEvent;
            private double m_StartTimeAsPerRuntime;
            private int m_AllEventsByTimeIndex = 0;
            private List<InputEventPtr> m_AllEventsByTime;

            internal ReplayController(InputEventTrace trace)
            {
                if (trace == null)
                    throw new ArgumentNullException(nameof(trace));

                m_EventTrace = trace;
            }

            /// <summary>
            /// Removes devices created by the controller when using <see cref="WithAllDevicesMappedToNewInstances"/>.
            /// </summary>
            public void Dispose()
            {
                InputSystem.onBeforeUpdate -= OnBeginFrame;
                finished = true;

                foreach (var device in m_CreatedDevices)
                    InputSystem.RemoveDevice(device);
                m_CreatedDevices = default;
            }

            /// <summary>
            /// Replay events recorded from <paramref name="recordedDevice"/> on device <paramref name="playbackDevice"/>.
            /// </summary>
            /// <param name="recordedDevice">Device events have been recorded from.</param>
            /// <param name="playbackDevice">Device events should be played back on.</param>
            /// <returns>The same ReplayController instance.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="recordedDevice"/> is <c>null</c> -or-
            /// <paramref name="playbackDevice"/> is <c>null</c>.</exception>
            /// <remarks>
            /// This method causes all events with a device ID (see <see cref="InputDevice.deviceId"/> and <see cref="InputEvent.deviceId"/>)
            /// corresponding to the one of <paramref cref="recordedDevice"/> to be queued with the device ID of <paramref name="playbackDevice"/>.
            /// </remarks>
            public ReplayController WithDeviceMappedFromTo(InputDevice recordedDevice, InputDevice playbackDevice)
            {
                if (recordedDevice == null)
                    throw new ArgumentNullException(nameof(recordedDevice));
                if (playbackDevice == null)
                    throw new ArgumentNullException(nameof(playbackDevice));

                WithDeviceMappedFromTo(recordedDevice.deviceId, playbackDevice.deviceId);
                return this;
            }

            /// <summary>
            /// Replace <see cref="InputEvent.deviceId"/> values of events that are equal to <paramref name="recordedDeviceId"/>
            /// with device ID <paramref name="playbackDeviceId"/>.
            /// </summary>
            /// <param name="recordedDeviceId"><see cref="InputDevice.deviceId"/> to map from.</param>
            /// <param name="playbackDeviceId"><see cref="InputDevice.deviceId"/> to map to.</param>
            /// <returns>The same ReplayController instance.</returns>
            public ReplayController WithDeviceMappedFromTo(int recordedDeviceId, int playbackDeviceId)
            {
                // If there's an existing mapping entry for the device, update it.
                for (var i = 0; i < m_DeviceIDMappings.length; ++i)
                {
                    if (m_DeviceIDMappings[i].Key != recordedDeviceId)
                        continue;

                    if (recordedDeviceId == playbackDeviceId) // Device mapped back to itself.
                        m_DeviceIDMappings.RemoveAtWithCapacity(i);
                    else
                        m_DeviceIDMappings[i] = new KeyValuePair<int, int>(recordedDeviceId, playbackDeviceId);

                    return this;
                }

                // Ignore if mapped to itself.
                if (recordedDeviceId == playbackDeviceId)
                    return this;

                // Record mapping.
                m_DeviceIDMappings.AppendWithCapacity(new KeyValuePair<int, int>(recordedDeviceId, playbackDeviceId));
                return this;
            }

            /// <summary>
            /// For all events, create new devices to replay the events on instead of replaying the events on existing devices.
            /// </summary>
            /// <returns>The same ReplayController instance.</returns>
            /// <remarks>
            /// Note that devices created by the <c>ReplayController</c> will stick around for as long as the replay
            /// controller is not disposed of. This means that multiple successive replays using the same <c>ReplayController</c>
            /// will replay the events on the same devices that were created on the first replay. It also means that in order
            /// to do away with the created devices, it is necessary to call <see cref="Dispose"/>.
            /// </remarks>
            /// <seealso cref="Dispose"/>
            /// <seealso cref="createdDevices"/>
            public ReplayController WithAllDevicesMappedToNewInstances()
            {
                m_CreateNewDevices = true;
                return this;
            }

            /// <summary>
            /// Invoke the given callback when playback finishes.
            /// </summary>
            /// <param name="action">A callback to invoke when playback finishes.</param>
            /// <returns>The same ReplayController instance.</returns>
            public ReplayController OnFinished(Action action)
            {
                m_OnFinished = action;
                return this;
            }

            /// <summary>
            /// Invoke the given callback when an event is about to be queued.
            /// </summary>
            /// <param name="action">A callback to invoke when an event is getting queued.</param>
            /// <returns>The same ReplayController instance.</returns>
            public ReplayController OnEvent(Action<InputEventPtr> action)
            {
                m_OnEvent = action;
                return this;
            }

            /// <summary>
            /// Takes the next event from the trace and queues it.
            /// </summary>
            /// <returns>The same ReplayController instance.</returns>
            /// <exception cref="InvalidOperationException">There are no more events in the <see cref="trace"/> -or- the only
            /// events left are frame marker events (see <see cref="InputEventTrace.FrameMarkerEvent"/>).</exception>
            /// <remarks>
            /// This method takes the next event at the current read position and queues it using <see cref="InputSystem.QueueEvent"/>.
            /// The read position is advanced past the taken event.
            ///
            /// Frame marker events (see <see cref="InputEventTrace.FrameMarkerEvent"/>) are skipped.
            /// </remarks>
            public ReplayController PlayOneEvent()
            {
                // Skip events until we hit something that isn't a frame marker.
                if (!MoveNext(true, out var eventPtr))
                    throw new InvalidOperationException("No more events");

                QueueEvent(eventPtr);

                return this;
            }

            ////TODO: OneFrame
            ////TODO: RewindOneEvent
            ////TODO: RewindOneFrame
            ////TODO: Stop

            /// <summary>
            /// Rewind playback all the way to the beginning of the event trace.
            /// </summary>
            /// <returns>The same ReplayController instance.</returns>
            public ReplayController Rewind()
            {
                m_Enumerator = default;
                m_AllEventsByTime = null;
                m_AllEventsByTimeIndex = -1;
                position = 0;
                return this;
            }

            /// <summary>
            /// Replay all frames one by one from the current playback position.
            /// </summary>
            /// <returns>The same ReplayController instance.</returns>
            /// <remarks>
            /// Events will be fed to the input system from within <see cref="InputSystem.onBeforeUpdate"/>. Each update
            /// will receive events for one frame.
            ///
            /// Note that for this method to correctly space out events and distribute them to frames, frame markers
            /// must be present in the trace (see <see cref="recordFrameMarkers"/>). If not present, all events will
            /// be fed into first frame.
            /// </remarks>
            /// <seealso cref="recordFrameMarkers"/>
            /// <seealso cref="InputSystem.onBeforeUpdate"/>
            /// <seealso cref="PlayAllEvents"/>
            /// <seealso cref="PlayAllEventsAccordingToTimestamps"/>
            public ReplayController PlayAllFramesOneByOne()
            {
                finished = false;
                InputSystem.onBeforeUpdate += OnBeginFrame;
                return this;
            }

            /// <summary>
            /// Go through all remaining event in the trace starting at the current read position and queue them using
            /// <see cref="InputSystem.QueueEvent"/>.
            /// </summary>
            /// <returns>The same ReplayController instance.</returns>
            /// <remarks>
            /// Unlike methods such as <see cref="PlayAllFramesOneByOne"/>, this method immediately queues events and immediately
            /// completes playback upon return from the method.
            /// </remarks>
            /// <seealso cref="PlayAllFramesOneByOne"/>
            /// <seealso cref="PlayAllEventsAccordingToTimestamps"/>
            public ReplayController PlayAllEvents()
            {
                finished = false;
                try
                {
                    while (MoveNext(true, out var eventPtr))
                        QueueEvent(eventPtr);
                }
                finally
                {
                    Finished();
                }
                return this;
            }

            /// <summary>
            /// Replay events in a way that tries to preserve the original timing sequence.
            /// </summary>
            /// <returns>The same ReplayController instance.</returns>
            /// <remarks>
            /// This method will take the current time as the starting time to which make all events
            /// relative to. Based on this time, it will try to correlate the original event timing
            /// with the timing of input updates as they happen. When successful, this will compensate
            /// for differences in frame timings compared to when input was recorded and instead queue
            /// input in frames that are closer to the original timing.
            ///
            /// Note that this method will perform one initial scan of the trace to determine a linear
            /// ordering of the events by time (the input system does not require any such ordering on the
            /// events in its queue and thus events in a trace, especially if there are multiple devices
            /// involved, may be out of order).
            /// </remarks>
            /// <seealso cref="PlayAllFramesOneByOne"/>
            /// <seealso cref="PlayAllEvents"/>
            public ReplayController PlayAllEventsAccordingToTimestamps()
            {
                // Sort remaining events by time.
                var eventsByTime = new List<InputEventPtr>();
                while (MoveNext(true, out var eventPtr))
                    eventsByTime.Add(eventPtr);
                eventsByTime.Sort((a, b) => a.time.CompareTo(b.time));
                m_Enumerator.Dispose();
                m_Enumerator = null;
                m_AllEventsByTime = eventsByTime;
                position = 0;

                // Start playback.
                finished = false;
                m_StartTimeAsPerFirstEvent = -1;
                m_AllEventsByTimeIndex = -1;
                InputSystem.onBeforeUpdate += OnBeginFrame;
                return this;
            }

            private void OnBeginFrame()
            {
                if (paused)
                    return;

                if (!MoveNext(false, out var currentEventPtr))
                {
                    if (m_AllEventsByTime == null || m_AllEventsByTimeIndex >= m_AllEventsByTime.Count)
                        Finished();
                    return;
                }

                // Check for empty frame (note: when playing back events by time, we won't see frame marker events
                // returned from MoveNext).
                if (currentEventPtr.type == FrameMarkerEvent)
                {
                    if (!MoveNext(false, out var nextEvent))
                    {
                        // Last frame.
                        Finished();
                        return;
                    }

                    // Check for empty frame.
                    if (nextEvent.type == FrameMarkerEvent)
                    {
                        --position;
                        m_Enumerator.m_Current = currentEventPtr;
                        return;
                    }

                    currentEventPtr = nextEvent;
                }

                // Inject our events into the frame.
                while (true)
                {
                    QueueEvent(currentEventPtr);

                    // Stop if we reach the end of the stream.
                    if (!MoveNext(false, out var nextEvent))
                    {
                        if (m_AllEventsByTime == null || m_AllEventsByTimeIndex >= m_AllEventsByTime.Count)
                            Finished();
                        break;
                    }

                    // Stop if we've reached the next frame (won't happen if we're playing events by time).
                    if (nextEvent.type == FrameMarkerEvent)
                    {
                        // Back up one event.
                        m_Enumerator.m_Current = currentEventPtr;
                        --position;
                        break;
                    }

                    currentEventPtr = nextEvent;
                }
            }

            private void Finished()
            {
                finished = true;
                InputSystem.onBeforeUpdate -= OnBeginFrame;
                m_OnFinished?.Invoke();
            }

            private void QueueEvent(InputEventPtr eventPtr)
            {
                // Shift time on event.
                var originalTimestamp = eventPtr.internalTime;
                if (m_AllEventsByTime != null)
                    eventPtr.internalTime = m_StartTimeAsPerRuntime + (eventPtr.internalTime - m_StartTimeAsPerFirstEvent);
                else
                    eventPtr.internalTime = InputRuntime.s_Instance.currentTime;

                // Remember original event ID. QueueEvent will automatically update the event ID
                // and actually do so in place.
                var originalEventId = eventPtr.id;

                // Map device ID.
                var originalDeviceId = eventPtr.deviceId;
                eventPtr.deviceId = ApplyDeviceMapping(originalDeviceId);

                // Notify.
                m_OnEvent?.Invoke(eventPtr);

                // Queue event.
                try
                {
                    InputSystem.QueueEvent(eventPtr);
                }
                finally
                {
                    // Restore modification we made to the event buffer.
                    eventPtr.internalTime = originalTimestamp;
                    eventPtr.id = originalEventId;
                    eventPtr.deviceId = originalDeviceId;
                }
            }

            private bool MoveNext(bool skipFrameEvents, out InputEventPtr eventPtr)
            {
                eventPtr = default;

                if (m_AllEventsByTime != null)
                {
                    if (m_AllEventsByTimeIndex + 1 >= m_AllEventsByTime.Count)
                    {
                        position = m_AllEventsByTime.Count;
                        m_AllEventsByTimeIndex = m_AllEventsByTime.Count;
                        return false;
                    }

                    if (m_AllEventsByTimeIndex < 0)
                    {
                        m_StartTimeAsPerFirstEvent = m_AllEventsByTime[0].internalTime;
                        m_StartTimeAsPerRuntime = InputRuntime.s_Instance.currentTime;
                    }
                    else if (m_AllEventsByTimeIndex < m_AllEventsByTime.Count - 1 &&
                             m_AllEventsByTime[m_AllEventsByTimeIndex + 1].internalTime > m_StartTimeAsPerFirstEvent + (InputRuntime.s_Instance.currentTime - m_StartTimeAsPerRuntime))
                    {
                        // We're queuing by original time and the next event isn't up yet,
                        // so early out.
                        return false;
                    }

                    ++m_AllEventsByTimeIndex;
                    ++position;
                    eventPtr = m_AllEventsByTime[m_AllEventsByTimeIndex];
                }
                else
                {
                    if (m_Enumerator == null)
                        m_Enumerator = new Enumerator(m_EventTrace);

                    do
                    {
                        if (!m_Enumerator.MoveNext())
                            return false;

                        ++position;
                        eventPtr = m_Enumerator.Current;
                    }
                    while (skipFrameEvents && eventPtr.type == FrameMarkerEvent);
                }

                return true;
            }

            private int ApplyDeviceMapping(int originalDeviceId)
            {
                // Look up in mappings.
                for (var i = 0; i < m_DeviceIDMappings.length; ++i)
                {
                    var entry = m_DeviceIDMappings[i];
                    if (entry.Key == originalDeviceId)
                        return entry.Value;
                }

                // Create device, if needed.
                if (m_CreateNewDevices)
                {
                    try
                    {
                        // Find device info.
                        var deviceIndex = m_EventTrace.deviceInfos.IndexOf(x => x.deviceId == originalDeviceId);
                        if (deviceIndex != -1)
                        {
                            var deviceInfo = m_EventTrace.deviceInfos[deviceIndex];

                            // If we don't have the layout, try to add it from the persisted layout info.
                            var layoutName = new InternedString(deviceInfo.layout);
                            if (!InputControlLayout.s_Layouts.HasLayout(layoutName))
                            {
                                if (string.IsNullOrEmpty(deviceInfo.m_FullLayoutJson))
                                    return originalDeviceId;

                                InputSystem.RegisterLayout(deviceInfo.m_FullLayoutJson);
                            }

                            // Create device.
                            var device = InputSystem.AddDevice(layoutName);
                            WithDeviceMappedFromTo(originalDeviceId, device.deviceId);
                            m_CreatedDevices.AppendWithCapacity(device);
                            return device.deviceId;
                        }
                    }
                    catch
                    {
                        // Swallow and just return originalDeviceId.
                    }
                }

                return originalDeviceId;
            }
        }

        /// <summary>
        /// Information about a device whose input has been captured in an <see cref="InputEventTrace"/>
        /// </summary>
        /// <seealso cref="InputEventTrace.deviceInfos"/>
        [Serializable]
        public struct DeviceInfo
        {
            /// <summary>
            /// Id of the device as stored in the events for the device.
            /// </summary>
            /// <seealso cref="InputDevice.deviceId"/>
            public int deviceId
            {
                get => m_DeviceId;
                set => m_DeviceId = value;
            }

            /// <summary>
            /// Name of the layout used by the device.
            /// </summary>
            /// <seealso cref="InputControl.layout"/>
            public string layout
            {
                get => m_Layout;
                set => m_Layout = value;
            }

            /// <summary>
            /// Tag for the format in which state for the device is stored.
            /// </summary>
            /// <seealso cref="InputControl.stateBlock"/>
            /// <seealso cref="InputStateBlock.format"/>
            public FourCC stateFormat
            {
                get => m_StateFormat;
                set => m_StateFormat = value;
            }

            /// <summary>
            /// Size of a full state snapshot of the device.
            /// </summary>
            public int stateSizeInBytes
            {
                get => m_StateSizeInBytes;
                set => m_StateSizeInBytes = value;
            }

            [SerializeField] internal int m_DeviceId;
            [SerializeField] internal string m_Layout;
            [SerializeField] internal FourCC m_StateFormat;
            [SerializeField] internal int m_StateSizeInBytes;
            [SerializeField] internal string m_FullLayoutJson;
        }
    }
}
