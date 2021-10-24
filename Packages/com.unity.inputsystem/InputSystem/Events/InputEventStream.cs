using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// The input event stream is a combination of the input event buffer passed from native code and an
    /// append buffer that is owned by the managed side. Events queued during update are added to the
    /// append buffer. To calling code, the two buffers look like a single coherent stream of events.
    /// Calling Advance will first step through the events from the native side, followed by any events
    /// that have been appended.
    /// </summary>
    internal unsafe struct InputEventStream
    {
        public bool isOpen => m_IsOpen;

        public int remainingEventCount => m_RemainingNativeEventCount + m_RemainingAppendEventCount;

        /// <summary>
        /// How many events were left in the native buffer during reading.
        /// </summary>
        public int numEventsRetainedInBuffer => m_NumEventsRetainedInBuffer;

        public InputEvent* currentEventPtr => m_RemainingNativeEventCount > 0
        ? m_CurrentNativeEventReadPtr
        : (m_RemainingAppendEventCount > 0 ? m_CurrentAppendEventReadPtr : null);

        public uint numBytesRetainedInBuffer =>
            (uint)((byte*)m_CurrentNativeEventWritePtr -
                (byte*)NativeArrayUnsafeUtility
                    .GetUnsafeBufferPointerWithoutChecks(m_NativeBuffer.data));

        public InputEventStream(ref InputEventBuffer eventBuffer, int maxAppendedEvents)
        {
            m_CurrentNativeEventWritePtr = m_CurrentNativeEventReadPtr =
                (InputEvent*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(eventBuffer.data);

            m_NativeBuffer = eventBuffer;
            m_RemainingNativeEventCount = m_NativeBuffer.eventCount;
            m_NumEventsRetainedInBuffer = 0;

            m_CurrentAppendEventReadPtr = m_CurrentAppendEventWritePtr = default;
            m_AppendBuffer = default;
            m_RemainingAppendEventCount = 0;
            m_MaxAppendedEvents = maxAppendedEvents;

            m_IsOpen = true;
        }

        public void Close(ref InputEventBuffer eventBuffer)
        {
            // If we have retained events, update event count and buffer size. If not, just reset.
            if (m_NumEventsRetainedInBuffer > 0)
            {
                var bufferPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_NativeBuffer.data);
                Debug.Assert((byte*)m_CurrentNativeEventWritePtr > (byte*)bufferPtr);
                var newBufferSize = (byte*)m_CurrentNativeEventWritePtr - (byte*)bufferPtr;
                m_NativeBuffer = new InputEventBuffer((InputEvent*)bufferPtr, m_NumEventsRetainedInBuffer, (int)newBufferSize,
                    (int)m_NativeBuffer.capacityInBytes);
            }
            else
            {
                m_NativeBuffer.Reset();
            }

            if (m_AppendBuffer.data.IsCreated)
                m_AppendBuffer.Dispose();

            eventBuffer = m_NativeBuffer;
            m_IsOpen = false;
        }

        public void CleanUpAfterException()
        {
            if (!isOpen)
                return;

            m_NativeBuffer.Reset();

            if (m_AppendBuffer.data.IsCreated)
                m_AppendBuffer.Dispose();

            m_IsOpen = false;
        }

        public void Write(InputEvent* eventPtr)
        {
            if (m_AppendBuffer.eventCount >= m_MaxAppendedEvents)
            {
                Debug.LogError($"Maximum number of queued events exceeded. Set the '{nameof(InputSettings.maxQueuedEventsPerUpdate)}' " +
                    $"setting to a higher value if you need to queue more events than this. " +
                    $"Current limit is '{m_MaxAppendedEvents}'.");
                return;
            }

            var wasAlreadyCreated = m_AppendBuffer.data.IsCreated;
            var oldBufferPtr = (byte*)m_AppendBuffer.bufferPtr.data;

            m_AppendBuffer.AppendEvent(eventPtr, allocator: Allocator.Temp);

            if (!wasAlreadyCreated)
            {
                m_CurrentAppendEventWritePtr = m_CurrentAppendEventReadPtr =
                    (InputEvent*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_AppendBuffer.data);
            }
            else
            {
                // AppendEvent can reallocate the buffer if it needs more space, so make sure the read and write pointers
                // point to the equivalent places in the new buffer.
                var newBufferPtr = (byte*)m_AppendBuffer.bufferPtr.data;
                if (oldBufferPtr != newBufferPtr)
                {
                    var currentWriteOffset = (byte*)m_CurrentAppendEventWritePtr - oldBufferPtr;
                    var currentReadOffset = (byte*)m_CurrentAppendEventReadPtr - oldBufferPtr;
                    m_CurrentAppendEventWritePtr = (InputEvent*)(newBufferPtr + currentWriteOffset);
                    m_CurrentAppendEventReadPtr = (InputEvent*)(newBufferPtr + currentReadOffset);
                }
            }

            m_RemainingAppendEventCount++;
        }

        public InputEvent* Advance(bool leaveEventInBuffer)
        {
            if (m_RemainingNativeEventCount > 0)
            {
                m_NativeBuffer.AdvanceToNextEvent(ref m_CurrentNativeEventReadPtr, ref m_CurrentNativeEventWritePtr,
                    ref m_NumEventsRetainedInBuffer, ref m_RemainingNativeEventCount, leaveEventInBuffer);
            }
            else if (m_RemainingAppendEventCount > 0)
            {
                var numEventRetained = 0;
                m_AppendBuffer.AdvanceToNextEvent(ref m_CurrentAppendEventReadPtr, ref m_CurrentAppendEventWritePtr,
                    ref numEventRetained, ref m_RemainingAppendEventCount, false);
            }

            return currentEventPtr;
        }

        /// <summary>
        /// Peeks next event in the stream
        /// </summary>
        public InputEvent* Peek()
        {
            // Advance will go to next event in m_NativeBuffer
            if (m_RemainingNativeEventCount > 1)
                return InputEvent.GetNextInMemory(m_CurrentNativeEventReadPtr);

            // Advance will decrement m_RemainingNativeEventCount to 0
            // and currentEventPtr will point to m_CurrentAppendEventReadPtr if any
            if (m_RemainingNativeEventCount == 1)
                return m_RemainingAppendEventCount > 0 ? m_CurrentAppendEventReadPtr : null;

            // Advance will go to next event in m_AppendBuffer
            if (m_RemainingAppendEventCount > 1)
                return InputEvent.GetNextInMemory(m_CurrentAppendEventReadPtr);

            // No next event
            return null;
        }

        private InputEventBuffer m_NativeBuffer;
        private InputEvent* m_CurrentNativeEventReadPtr;
        private InputEvent* m_CurrentNativeEventWritePtr;
        private int m_RemainingNativeEventCount;
        private readonly int m_MaxAppendedEvents;

        // During Update, new events that are queued will be added to the append buffer
        private InputEventBuffer m_AppendBuffer;
        private InputEvent* m_CurrentAppendEventReadPtr;
        private InputEvent* m_CurrentAppendEventWritePtr;
        private int m_RemainingAppendEventCount;

        // When timeslicing events or in before-render updates, we may be leaving events in the buffer
        // for later processing. We do this by compacting the event buffer and moving events down such
        // that the events we leave in the buffer form one contiguous chunk of memory at the beginning
        // of the buffer.
        private int m_NumEventsRetainedInBuffer;
        private bool m_IsOpen;
    }
}
