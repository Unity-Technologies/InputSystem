using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

////TODO: batch append method

////TODO: switch to NativeArray long length (think we have it in Unity 2018.3)

////REVIEW: can we get rid of kBufferSizeUnknown and force size to always be known? (think this would have to wait until
////        the native changes have landed in 2018.3)

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A buffer of raw memory holding a sequence of <see cref="InputEvent">input events</see>.
    /// </summary>
    /// <remarks>
    /// Note that event buffers are not thread-safe. It is not safe to write events to the buffer
    /// concurrently from multiple threads. It is, however, safe to traverse the contents of an
    /// existing buffer from multiple threads as long as it is not mutated at the same time.
    /// </remarks>
    public unsafe struct InputEventBuffer : IEnumerable<InputEventPtr>, IDisposable, ICloneable
    {
        public const long BufferSizeUnknown = -1;

        /// <summary>
        /// Total number of events in the buffer.
        /// </summary>
        /// <value>Number of events currently in the buffer.</value>
        public int eventCount => m_EventCount;

        /// <summary>
        /// Size of the used portion of the buffer in bytes. Use <see cref="capacityInBytes"/> to
        /// get the total allocated size.
        /// </summary>
        /// <value>Used size of buffer in bytes.</value>
        /// <remarks>
        /// If the size is not known, returns <see cref="BufferSizeUnknown"/>.
        ///
        /// Note that the size does not usually correspond to <see cref="eventCount"/> times <c>sizeof(InputEvent)</c>.
        /// as <see cref="InputEvent"/> instances are variable in size.
        /// </remarks>
        public long sizeInBytes => m_SizeInBytes;

        /// <summary>
        /// Total size of allocated memory in bytes. This value minus <see cref="sizeInBytes"/> is the
        /// spare capacity of the buffer. Will never be less than <see cref="sizeInBytes"/>.
        /// </summary>
        /// <value>Size of allocated memory in bytes.</value>
        /// <remarks>
        /// A buffer's capacity determines how much event data can be written to the buffer before it has to be
        /// reallocated.
        /// </remarks>
        public long capacityInBytes
        {
            get
            {
                if (!m_Buffer.IsCreated)
                    return 0;

                return m_Buffer.Length;
            }
        }

        /// <summary>
        /// The raw underlying memory buffer.
        /// </summary>
        /// <value>Underlying buffer of unmanaged memory.</value>
        public NativeArray<byte> data => m_Buffer;

        /// <summary>
        /// Pointer to the first event in the buffer.
        /// </summary>
        /// <value>Pointer to first event in buffer.</value>
        public InputEventPtr bufferPtr
        {
            // When using ConvertExistingDataToNativeArray, the NativeArray isn't getting a "safety handle" (seems like a bug)
            // and calling GetUnsafeReadOnlyPtr() will result in a NullReferenceException. Get the pointer without checks here.
            get { return (InputEvent*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer); }
        }

        /// <summary>
        /// Construct an event buffer using the given memory block containing <see cref="InputEvent"/>s.
        /// </summary>
        /// <param name="eventPtr">A buffer containing <paramref name="eventCount"/> number of input events. The
        /// individual events in the buffer are variable-sized (depending on the type of each event).</param>
        /// <param name="eventCount">The number of events in <paramref name="eventPtr"/>. Can be zero.</param>
        /// <param name="sizeInBytes">Total number of bytes of event data in the memory block pointed to by <paramref name="eventPtr"/>.
        /// If -1 (default), the size of the actual event data in the buffer is considered unknown and has to be determined by walking
        /// <paramref name="eventCount"/> number of events (due to the variable size of each event).</param>
        /// <param name="capacityInBytes">The total size of the memory block allocated at <paramref name="eventPtr"/>. If this
        /// is larger than <paramref name="sizeInBytes"/>, additional events can be appended to the buffer until the capacity
        /// is exhausted. If this is -1 (default), the capacity is considered unknown and no additional events can be
        /// appended to the buffer.</param>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is <c>null</c> and <paramref name="eventCount"/> is not zero
        /// -or- <paramref name="capacityInBytes"/> is less than <paramref name="sizeInBytes"/>.</exception>
        public InputEventBuffer(InputEvent* eventPtr, int eventCount, int sizeInBytes = -1, int capacityInBytes = -1)
            : this()
        {
            if (eventPtr == null && eventCount != 0)
                throw new ArgumentException("eventPtr is NULL but eventCount is != 0", nameof(eventCount));
            if (capacityInBytes != 0 && capacityInBytes < sizeInBytes)
                throw new ArgumentException($"capacity({capacityInBytes}) cannot be smaller than size({sizeInBytes})",
                    nameof(capacityInBytes));

            if (eventPtr != null)
            {
                if (capacityInBytes < 0)
                    capacityInBytes = sizeInBytes;

                m_Buffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(eventPtr,
                    capacityInBytes > 0 ? capacityInBytes : 0, Allocator.None);
                m_SizeInBytes = sizeInBytes >= 0 ? sizeInBytes : BufferSizeUnknown;
                m_EventCount = eventCount;
                m_WeOwnTheBuffer = false;
            }
        }

        /// <summary>
        /// Construct an event buffer using the array containing <see cref="InputEvent"/>s.
        /// </summary>
        /// <param name="buffer">A native array containing <paramref name="eventCount"/> number of input events. The
        /// individual events in the buffer are variable-sized (depending on the type of each event).</param>
        /// <param name="eventCount">The number of events in <paramref name="buffer"/>. Can be zero.</param>
        /// <param name="sizeInBytes">Total number of bytes of event data in the <paramref cref="buffer"/>.
        /// If -1 (default), the size of the actual event data in <paramref name="buffer"/> is considered unknown and has to be determined by walking
        /// <paramref name="eventCount"/> number of events (due to the variable size of each event).</param>
        /// <param name="transferNativeArrayOwnership">If true, ownership of the <c>NativeArray</c> given by <paramref name="buffer"/> is
        /// transferred to the <c>InputEventBuffer</c>. Calling <see cref="Dispose"/> will deallocate the array. Also, <see cref="AllocateEvent"/>
        /// may re-allocate the array.</param>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> has no memory allocated but <paramref name="eventCount"/> is not zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeInBytes"/> is greater than the total length allocated for
        /// <paramref name="buffer"/>.</exception>
        public InputEventBuffer(NativeArray<byte> buffer, int eventCount, int sizeInBytes = -1, bool transferNativeArrayOwnership = false)
        {
            if (eventCount > 0 && !buffer.IsCreated)
                throw new ArgumentException("buffer has no data but eventCount is > 0", nameof(eventCount));
            if (sizeInBytes > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(sizeInBytes));

            m_Buffer = buffer;
            m_WeOwnTheBuffer = transferNativeArrayOwnership;
            m_SizeInBytes = sizeInBytes >= 0 ? sizeInBytes : buffer.Length;
            m_EventCount = eventCount;
        }

        /// <summary>
        /// Append a new event to the end of the buffer by copying the event from <paramref name="eventPtr"/>.
        /// </summary>
        /// <param name="eventPtr">Data of the event to store in the buffer. This will be copied in full as
        /// per <see cref="InputEvent.sizeInBytes"/> found in the event's header.</param>
        /// <param name="capacityIncrementInBytes">If the buffer needs to be reallocated to accommodate the event, number of
        /// bytes to grow the buffer by.</param>
        /// <param name="allocator">If the buffer needs to be reallocated to accommodate the event, the type of allocation to
        /// use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="eventPtr"/> is <c>null</c>.</exception>
        /// <remarks>
        /// If the buffer's current capacity (see <see cref="capacityInBytes"/>) is smaller than <see cref="InputEvent.sizeInBytes"/>
        /// of the given event, the buffer will be reallocated.
        /// </remarks>
        public void AppendEvent(InputEvent* eventPtr, int capacityIncrementInBytes = 2048, Allocator allocator = Allocator.Persistent)
        {
            if (eventPtr == null)
                throw new ArgumentNullException(nameof(eventPtr));

            // Allocate space.
            var eventSizeInBytes = eventPtr->sizeInBytes;
            var destinationPtr = AllocateEvent((int)eventSizeInBytes, capacityIncrementInBytes, allocator);

            // Copy event.
            UnsafeUtility.MemCpy(destinationPtr, eventPtr, eventSizeInBytes);
        }

        /// <summary>
        /// Make space for an event of <paramref name="sizeInBytes"/> bytes and return a pointer to
        /// the memory for the event.
        /// </summary>
        /// <param name="sizeInBytes">Number of bytes to make available for the event including the event header (see <see cref="InputEvent"/>).</param>
        /// <param name="capacityIncrementInBytes">If the buffer needs to be reallocated to accommodate the event, number of
        ///     bytes to grow the buffer by.</param>
        /// <param name="allocator">If the buffer needs to be reallocated to accommodate the event, the type of allocation to
        /// use.</param>
        /// <returns>A pointer to a block of memory in <see cref="bufferPtr"/>. Store the event data here.</returns>
        /// <exception cref="ArgumentException"><paramref name="sizeInBytes"/> is less than the size needed for the
        /// header of an <see cref="InputEvent"/>. Will automatically be aligned to a multiple of 4.</exception>
        /// <remarks>
        /// Only <see cref="InputEvent.sizeInBytes"/> is initialized by this method. No other fields from the event's
        /// header are touched.
        ///
        /// The event will be appended to the buffer after the last event currently in the buffer (if any).
        /// </remarks>
        public InputEvent* AllocateEvent(int sizeInBytes, int capacityIncrementInBytes = 2048, Allocator allocator = Allocator.Persistent)
        {
            if (sizeInBytes < InputEvent.kBaseEventSize)
                throw new ArgumentException(
                    $"sizeInBytes must be >= sizeof(InputEvent) == {InputEvent.kBaseEventSize} (was {sizeInBytes})",
                    nameof(sizeInBytes));

            var alignedSizeInBytes = sizeInBytes.AlignToMultipleOf(InputEvent.kAlignment);

            // See if we need to enlarge our buffer.
            var necessaryCapacity = m_SizeInBytes + alignedSizeInBytes;
            var currentCapacity = capacityInBytes;
            if (currentCapacity < necessaryCapacity)
            {
                // Yes, so reallocate.

                var newCapacity = necessaryCapacity.AlignToMultipleOf(capacityIncrementInBytes);
                if (newCapacity > int.MaxValue)
                    throw new NotImplementedException("NativeArray long support");
                var newBuffer =
                    new NativeArray<byte>((int)newCapacity, allocator, NativeArrayOptions.ClearMemory);

                if (m_Buffer.IsCreated)
                {
                    UnsafeUtility.MemCpy(newBuffer.GetUnsafePtr(),
                        NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer),
                        this.sizeInBytes);

                    if (m_WeOwnTheBuffer)
                        m_Buffer.Dispose();
                }

                m_Buffer = newBuffer;
                m_WeOwnTheBuffer = true;
            }

            var eventPtr = (InputEvent*)((byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer) + m_SizeInBytes);
            eventPtr->sizeInBytes = (uint)sizeInBytes;
            m_SizeInBytes += alignedSizeInBytes;
            ++m_EventCount;

            return eventPtr;
        }

        /// <summary>
        /// Whether the given event pointer refers to data within the event buffer.
        /// </summary>
        /// <param name="eventPtr"></param>
        /// <returns></returns>
        /// <remarks>
        /// Note that this method does NOT check whether the given pointer points to an actual
        /// event in the buffer. It solely performs a pointer out-of-bounds check.
        ///
        /// Also note that if the size of the memory buffer is unknown (<see cref="BufferSizeUnknown"/>,
        /// only a lower-bounds check is performed.
        /// </remarks>
        public bool Contains(InputEvent* eventPtr)
        {
            if (eventPtr == null)
                return false;

            if (sizeInBytes == 0)
                return false;

            var bufferPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(data);
            if (eventPtr < bufferPtr)
                return false;

            if (sizeInBytes != BufferSizeUnknown && eventPtr >= (byte*)bufferPtr + sizeInBytes)
                return false;

            return true;
        }

        public void Reset()
        {
            m_EventCount = 0;
            if (m_SizeInBytes != BufferSizeUnknown)
                m_SizeInBytes = 0;
        }

        /// <summary>
        /// Advance the read position to the next event in the buffer, preserving or not preserving the
        /// current event depending on <paramref name="leaveEventInBuffer"/>.
        /// </summary>
        /// <param name="currentReadPos"></param>
        /// <param name="currentWritePos"></param>
        /// <param name="numEventsRetainedInBuffer"></param>
        /// <param name="numRemainingEvents"></param>
        /// <param name="leaveEventInBuffer"></param>
        /// <remarks>
        /// This method MUST ONLY BE CALLED if the current event has been fully processed. If the at <paramref name="currentWritePos"/>
        /// is smaller than the current event, then this method will OVERWRITE parts or all of the current event.
        /// </remarks>
        internal void AdvanceToNextEvent(ref InputEvent* currentReadPos,
            ref InputEvent* currentWritePos, ref int numEventsRetainedInBuffer,
            ref int numRemainingEvents, bool leaveEventInBuffer)
        {
            Debug.Assert(currentReadPos >= currentWritePos, "Current write position is beyond read position");

            // Get new read position *before* potentially moving the current event so that we don't
            // end up overwriting the data we need to find the next event in memory.
            var newReadPos = currentReadPos;
            if (numRemainingEvents > 1)
            {
                // Don't perform safety check in non-debug builds.
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                newReadPos = InputEvent.GetNextInMemoryChecked(currentReadPos, ref this);
                #else
                newReadPos = InputEvent.GetNextInMemory(currentReadPos);
                #endif
            }

            // If the current event should be left in the buffer, advance write position.
            if (leaveEventInBuffer)
            {
                Debug.Assert(Contains(currentWritePos), "Current write position should be contained in buffer");

                // Move down in buffer if read and write pos have deviated from each other.
                var numBytes = currentReadPos->sizeInBytes;
                if (currentReadPos != currentWritePos)
                    UnsafeUtility.MemMove(currentWritePos, currentReadPos, numBytes);
                currentWritePos = (InputEvent*)((byte*)currentWritePos + numBytes.AlignToMultipleOf(4));
                ++numEventsRetainedInBuffer;
            }

            currentReadPos = newReadPos;
            --numRemainingEvents;
        }

        public IEnumerator<InputEventPtr> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            // Nothing to do if we don't actually own the memory.
            if (!m_WeOwnTheBuffer)
                return;

            Debug.Assert(m_Buffer.IsCreated, "Buffer has not been created");

            m_Buffer.Dispose();
            m_WeOwnTheBuffer = false;
            m_SizeInBytes = 0;
            m_EventCount = 0;
        }

        public InputEventBuffer Clone()
        {
            var clone = new InputEventBuffer();
            if (m_Buffer.IsCreated)
            {
                clone.m_Buffer = new NativeArray<byte>(m_Buffer.Length, Allocator.Persistent);
                clone.m_Buffer.CopyFrom(m_Buffer);
                clone.m_WeOwnTheBuffer = true;
            }
            clone.m_SizeInBytes = m_SizeInBytes;
            clone.m_EventCount = m_EventCount;
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private NativeArray<byte> m_Buffer;
        private long m_SizeInBytes;
        private int m_EventCount;
        private bool m_WeOwnTheBuffer; ////FIXME: what we really want is access to NativeArray's allocator label

        private struct Enumerator : IEnumerator<InputEventPtr>
        {
            private readonly InputEvent* m_Buffer;
            private readonly int m_EventCount;
            private InputEvent* m_CurrentEvent;
            private int m_CurrentIndex;

            public Enumerator(InputEventBuffer buffer)
            {
                m_Buffer = buffer.bufferPtr;
                m_EventCount = buffer.m_EventCount;
                m_CurrentEvent = null;
                m_CurrentIndex = 0;
            }

            public bool MoveNext()
            {
                if (m_CurrentIndex == m_EventCount)
                    return false;

                if (m_CurrentEvent == null)
                {
                    m_CurrentEvent = m_Buffer;
                    return m_CurrentEvent != null;
                }

                Debug.Assert(m_CurrentEvent != null, "Current event must not be null");

                ++m_CurrentIndex;
                if (m_CurrentIndex == m_EventCount)
                    return false;

                m_CurrentEvent = InputEvent.GetNextInMemory(m_CurrentEvent);
                return true;
            }

            public void Reset()
            {
                m_CurrentEvent = null;
                m_CurrentIndex = 0;
            }

            public void Dispose()
            {
            }

            public InputEventPtr Current => m_CurrentEvent;

            object IEnumerator.Current => Current;
        }
    }
}
