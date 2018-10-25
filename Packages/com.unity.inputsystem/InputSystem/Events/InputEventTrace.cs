using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;

////FIXME: the ring buffer insertion and/or traversal logic is still buggy :(

////TODO: Use InputEventBuffer

namespace UnityEngine.Experimental.Input.LowLevel
{
    // Helper to simplify recording events. Can record events for a specific device
    // or all events coming in.
    [Serializable]
    public class InputEventTrace : IDisposable, IEnumerable<InputEventPtr>
    {
        public const int kDefaultBufferSize = 1024 * 1024;

        // Set device to record events for. Set to kInvalidDeviceId by default
        // in which case events from all devices are recorded.
        public int deviceId
        {
            get { return m_DeviceId; }
            set { m_DeviceId = value; }
        }

        public bool enabled
        {
            get { return m_Enabled; }
        }

        public event Action<InputEventPtr> onEvent
        {
            add { m_EventListeners.Append(value); }
            remove { m_EventListeners.Remove(value); }
        }

        // Create a disabled event trace that does not perform any allocation
        // yet. An event trace only starts consuming resources the first time
        // it is enabled.
        public InputEventTrace(int bufferSize = kDefaultBufferSize)
        {
            m_EventBufferSize = (uint)bufferSize;
        }

        public void Clear()
        {
            m_EventBufferHead = m_EventBufferTail = IntPtr.Zero;
        }

        public void Enable()
        {
            if (m_Enabled)
                return;

            if (m_EventBuffer == IntPtr.Zero)
                Allocate();

            InputSystem.onEvent += OnInputEvent;
            m_Enabled = true;
        }

        public void Disable()
        {
            if (!m_Enabled)
                return;

            InputSystem.onEvent -= OnInputEvent;
            m_Enabled = false;
        }

        public unsafe bool GetNextEvent(ref InputEventPtr current)
        {
            if (m_EventBuffer == IntPtr.Zero)
                return false;

            // If head is null, tail is too and it means there's nothing in the
            // buffer yet.
            if (m_EventBufferHead == IntPtr.Zero)
                return false;

            // If current is null, start iterating at head.
            if (!current.valid)
            {
                current = new InputEventPtr((InputEvent*)m_EventBufferHead);
                return true;
            }

            // Otherwise feel our way forward.

            var nextEvent = new IntPtr(current.data.ToInt64() + current.sizeInBytes);
            var endOfBuffer = new IntPtr(m_EventBuffer.ToInt64() + m_EventBufferSize);

            // If we've run into our tail, there's no more events.
            if (nextEvent.ToInt64() == m_EventBufferTail.ToInt64())
                return false;

            // If we've reached blank space at the end of the buffer, wrap
            // around to the beginning. In this scenario there must be an event
            // at the beginning of the buffer; tail won't position itself at
            // m_EventBuffer.
            if (endOfBuffer.ToInt64() - nextEvent.ToInt64() < InputEvent.kBaseEventSize ||
                ((InputEvent*)nextEvent)->sizeInBytes == 0)
            {
                nextEvent = m_EventBuffer;
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

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        ~InputEventTrace()
        {
            Release();
        }

        // We want to make sure that it's not possible to iterate with an enumerable over
        // a trace that is being changed so we bump this counter every time we modify the
        // buffer and check in the enumerator that the counts match.
        [NonSerialized] private int m_ChangeCounter;
        [NonSerialized] private bool m_Enabled;

        [SerializeField] private int m_DeviceId = InputDevice.kInvalidDeviceId;
        [SerializeField] private InlinedArray<Action<InputEventPtr>> m_EventListeners;

        // Buffer for storing event trace. Allocated in native so that we can survive a
        // domain reload without losing event traces.
        [SerializeField] private uint m_EventBufferSize;
        [SerializeField] private IntPtr m_EventBuffer;
        [SerializeField] private IntPtr m_EventBufferHead;
        [SerializeField] private IntPtr m_EventBufferTail;

        private unsafe void Allocate()
        {
            m_EventBuffer = (IntPtr)UnsafeUtility.Malloc(m_EventBufferSize, 4, Allocator.Persistent);
        }

        private unsafe void Release()
        {
            Disable();

            if (m_EventBuffer != IntPtr.Zero)
                UnsafeUtility.Free(m_EventBuffer.ToPointer(), Allocator.Persistent);

            m_EventBuffer = IntPtr.Zero;
            m_EventBufferHead = IntPtr.Zero;
            m_EventBufferTail = IntPtr.Zero;
        }

        private unsafe void OnInputEvent(InputEventPtr inputEvent)
        {
            // Ignore if the event isn't for our device.
            if (m_DeviceId != InputDevice.kInvalidDeviceId && inputEvent.deviceId != m_DeviceId)
                return;

            // This shouldn't happen but ignore the event if we're not tracing.
            if (m_EventBuffer == IntPtr.Zero)
                return;

            var eventSize = (uint)inputEvent.sizeInBytes;
            var eventData = inputEvent.data;

            // Make sure we can fit the event at all.
            if (eventSize > m_EventBufferSize)
                return;

            Profiler.BeginSample("InputEventTrace");

            // Make room in the buffer for the event.
            IntPtr buffer;
            if (m_EventBufferTail == IntPtr.Zero)
            {
                // First event in buffer.
                buffer = m_EventBuffer;
                m_EventBufferHead = m_EventBuffer;
                m_EventBufferTail = new IntPtr(buffer.ToInt64() + eventSize);
            }
            else
            {
                var newTail = new IntPtr(m_EventBufferTail.ToInt64() + eventSize);

                var newTailOvertakesHead = newTail.ToInt64() > m_EventBufferHead.ToInt64() && m_EventBufferHead != m_EventBuffer;
                var newTailGoesPastEndOfBuffer = newTail.ToInt64() > (m_EventBuffer.ToInt64() + m_EventBufferSize);

                // If tail goes out of bounds, go back to beginning of buffer.
                if (newTailGoesPastEndOfBuffer)
                {
                    // Make sure head isn't trying to advance into gap we may be leaving at the end of the
                    // buffer by wiping the space if it could fit an event.
                    var spaceLeft = m_EventBufferSize - (m_EventBufferTail.ToInt64() - m_EventBuffer.ToInt64());
                    if (spaceLeft >= InputEvent.kBaseEventSize)
                        UnsafeUtility.MemClear(m_EventBufferTail.ToPointer(), InputEvent.kBaseEventSize);

                    m_EventBufferTail = m_EventBuffer;
                    newTail = new IntPtr(m_EventBuffer.ToInt64() + eventSize);

                    // Recheck whether we're overtaking head.
                    newTailOvertakesHead = newTail.ToInt64() > m_EventBufferHead.ToInt64();
                }

                // If the new tail runs into head, bump head as many times as we need to
                // make room for the event. Head may itself wrap around here.
                if (newTailOvertakesHead)
                {
                    var newHead = (byte*)m_EventBufferHead;
                    var endOfBufferMinusOneEvent =
                        (byte*)m_EventBuffer + m_EventBufferSize - InputEvent.kBaseEventSize;

                    while (newHead < (byte*)newTail)
                    {
                        newHead += ((InputEvent*)newHead)->sizeInBytes;
                        if (newHead > endOfBufferMinusOneEvent || ((InputEvent*)newHead)->sizeInBytes == 0)
                        {
                            newHead = (byte*)m_EventBuffer;
                            break;
                        }
                    }

                    m_EventBufferHead = new IntPtr(newHead);
                }

                buffer = m_EventBufferTail;
                m_EventBufferTail = newTail;
            }

            // Copy data to buffer.
            UnsafeUtility.MemCpy(buffer.ToPointer(), eventData.ToPointer(), eventSize);
            ++m_ChangeCounter;

            // Notify listeners.
            for (var i = 0; i < m_EventListeners.length; ++i)
                m_EventListeners[i](new InputEventPtr((InputEvent*)buffer));

            Profiler.EndSample();
        }

        private class Enumerator : IEnumerator<InputEventPtr>
        {
            private InputEventTrace m_Trace;
            private readonly int m_ChangeCounter;
            private InputEventPtr m_Current;

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
                m_Current = new InputEventPtr();
            }

            public InputEventPtr Current
            {
                get { return m_Current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}
