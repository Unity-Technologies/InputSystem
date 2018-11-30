using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Utilities;

////TODO: batch append method

////TODO: switch to NativeArray long length (think we have it in Unity 2018.3)

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A buffer of raw memory holding a sequence of <see cref="InputEvent">input events</see>.
    /// </summary>
    /// <remarks>
    /// Note that event buffers are not thread-safe. It is not safe to write events to the buffer
    /// concurrently from multiple threads. It is, however, safe to traverse the contents of an
    /// existing buffer from multiple threads as long as it is not mutated at the same time.
    /// </remarks>
    public unsafe struct InputEventBuffer : IEnumerable<InputEventPtr>, IDisposable
    {
        public const long kBufferSizeUnknown = -1;

        /// <summary>
        /// Total number of events in the buffer.
        /// </summary>
        public int eventCount
        {
            get { return m_EventCount; }
        }

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        /// <remarks>
        /// If the size is not known, returns <see cref="kBufferSizeUnknown"/>.
        ///
        /// Note that the size does not usually correspond to <see cref="eventCount"/> times <c>sizeof(InputEvent)</c>.
        /// <see cref="InputEvent">Input events</see> are variable in size.
        /// </remarks>
        public long sizeInBytes
        {
            get { return m_BufferEnd; }
        }

        /// <summary>
        /// Amount of unused bytes in the currently allocated buffer.
        /// </summary>
        /// <remarks>
        /// A buffer's capacity determines how much event data can be written to the buffer before it has to be
        /// reallocated.
        /// </remarks>
        public long capacityInBytes
        {
            get
            {
                if (!m_Buffer.IsCreated || m_BufferEnd == kBufferSizeUnknown)
                    return 0;

                return m_Buffer.Length - m_BufferEnd;
            }
            set { throw new NotImplementedException(); }
        }

        public NativeArray<byte> data
        {
            get { return m_Buffer; }
            set { throw new NotImplementedException(); }
        }

        public InputEventPtr bufferPtr
        {
            // When using ConvertExistingDataToNativeArray, the NativeArray isn't getting a "safety handle" (seems like a bug)
            // and calling GetUnsafeReadOnlyPtr() will result in a NullReferenceException. Get the pointer without checks here.
            get { return (InputEvent*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Buffer); }
        }

        public InputEventBuffer(InputEvent* eventPtr, int eventCount)
            : this()
        {
            if (eventPtr == null && eventCount != 0)
                throw new ArgumentException("eventPtr is NULL but eventCount is != 0", "eventCount");

            if (eventPtr != null)
            {
                m_Buffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(eventPtr, 0, Allocator.None);
                m_BufferEnd = kBufferSizeUnknown;
                m_EventCount = eventCount;
                m_WeOwnTheBuffer = false;
            }
        }

        public InputEventBuffer(NativeArray<byte> buffer, int eventCount, int sizeInBytes = -1)
        {
            if (eventCount > 0 && !buffer.IsCreated)
                throw new ArgumentException("buffer has no data but eventCount is > 0", "eventCount");
            if (sizeInBytes > buffer.Length)
                throw new ArgumentOutOfRangeException("sizeInBytes");

            m_Buffer = buffer;
            m_WeOwnTheBuffer = false;
            m_BufferEnd = sizeInBytes >= 0 ? sizeInBytes : buffer.Length;
            m_EventCount = eventCount;
        }

        /// <summary>
        /// Append a new event to the end of the buffer.
        /// </summary>
        /// <param name="eventPtr"></param>
        /// <param name="capacityIncrementInBytes"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// If the buffer's current <see cref="capacityInBytes">capacity</see> is smaller than the <see cref="InputEvent.sizeInBytes">
        /// size</see> of the given <paramref name="eventPtr">event</paramref>,
        /// </remarks>
        public void AppendEvent(InputEvent* eventPtr, int capacityIncrementInBytes = 2048)
        {
            if (eventPtr == null)
                throw new ArgumentNullException("eventPtr");

            // Allocate space.
            var eventSizeInBytes = eventPtr->sizeInBytes;
            var destinationPtr = AllocateEvent((int)eventSizeInBytes, capacityIncrementInBytes);

            // Copy event.
            UnsafeUtility.MemCpy(destinationPtr, eventPtr, eventSizeInBytes);
        }

        public InputEvent* AllocateEvent(int sizeInBytes, int capacityIncrementInBytes = 2048)
        {
            if (sizeInBytes < InputEvent.kBaseEventSize)
                throw new ArgumentException(
                    string.Format("sizeInBytes must be >= sizeof(InputEvent) == {0} (was {1})",
                        InputEvent.kBaseEventSize, sizeInBytes),
                    "sizeInBytes");

            var alignedSizeInBytes = NumberHelpers.AlignToMultiple(sizeInBytes, InputEvent.kAlignment);

            // See if we need to enlarge our buffer.
            var currentCapacity = capacityInBytes;
            if (currentCapacity < alignedSizeInBytes)
            {
                // Yes, so reallocate.
                var newCapacity = Math.Max(currentCapacity + capacityIncrementInBytes,
                    currentCapacity + alignedSizeInBytes);
                var newSize = this.sizeInBytes + newCapacity;
                if (newSize > int.MaxValue)
                    throw new NotImplementedException("NativeArray long support");
                var newBuffer =
                    new NativeArray<byte>((int)newSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);

                if (m_Buffer.IsCreated)
                    UnsafeUtility.MemCpy(newBuffer.GetUnsafePtr(), m_Buffer.GetUnsafeReadOnlyPtr(), this.sizeInBytes);
                else
                    m_BufferEnd = 0;

                if (m_WeOwnTheBuffer)
                    m_Buffer.Dispose();
                m_Buffer = newBuffer;
                m_WeOwnTheBuffer = true;
            }

            var eventPtr = (InputEvent*)((byte*)m_Buffer.GetUnsafePtr() + m_BufferEnd);
            eventPtr->sizeInBytes = (uint)sizeInBytes;
            m_BufferEnd += alignedSizeInBytes;
            ++m_EventCount;

            return eventPtr;
        }

        public void Reset()
        {
            m_EventCount = 0;
            if (m_BufferEnd != kBufferSizeUnknown)
                m_BufferEnd = 0;
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

            Debug.Assert(m_Buffer.IsCreated);

            m_Buffer.Dispose();
            m_WeOwnTheBuffer = false;
            m_BufferEnd = 0;
            m_EventCount = 0;
        }

        private NativeArray<byte> m_Buffer;
        private long m_BufferEnd;
        private int m_EventCount;
        private bool m_WeOwnTheBuffer; ////FIXME: what we really want is access to NativeArray's allocator label

        internal struct Enumerator : IEnumerator<InputEventPtr>
        {
            private InputEvent* m_Buffer;
            private InputEvent* m_CurrentEvent;
            private int m_CurrentIndex;
            private int m_EventCount;

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

                Debug.Assert(m_CurrentEvent != null);

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

            public InputEventPtr Current
            {
                get { return m_CurrentEvent; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
