using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [Obsolete("IMECompositionEvent is obsolete, please use IMECompositionEventVariableSize")]
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + sizeof(int) + (sizeof(char) * kIMECharBufferSize))]
    public unsafe struct IMECompositionEvent : IInputEventTypeInfo
    {
        // These needs to match the native ImeCompositionStringInputEventData settings
        internal const int kIMECharBufferSize = 64;
        public const int Type = 0x494D4553;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        private int length;

        [FieldOffset(InputEvent.kBaseEventSize + sizeof(int))]
        private fixed char buffer[kIMECharBufferSize];

        public IMECompositionString compositionString
        {
            get
            {
                fixed(char* ptr = buffer)
                return new IMECompositionString(ptr, length);
            }
        }

        public FourCC typeStatic => Type;

        public static IMECompositionEvent Create(int deviceId, string compositionString, double time)
        {
            var inputEvent = new IMECompositionEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + sizeof(int) + (sizeof(char) * kIMECharBufferSize), deviceId, time);
            inputEvent.length = compositionString.Length > kIMECharBufferSize ? kIMECharBufferSize : compositionString.Length;
            fixed(char* dst = compositionString)
            fixed(char* src = compositionString)
            UnsafeUtility.MemCpy(dst, src, inputEvent.length * sizeof(char));
            return inputEvent;
        }
    }

    /// <summary>
    /// A specialized event that contains the current IME Composition string, if IME is enabled and active.
    /// This event contains the entire current string to date, and once a new composition is submitted will send a blank string event.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + sizeof(int))]
    public struct IMECompositionEventVariableSize : IInputEventTypeInfo
    {
        // Before we had 0x494D4553 which corresponds to ImeCompositionStringInputEventData fixed size event with 64 character payload.
        // 0x494D4543 corresponds to ImeCompositionInputEventData and is a different event which provides variable size array of characters after the event.
        public const int Type = 0x494D4543;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        internal int length;

        internal static unsafe char* GetCharsPtr(IMECompositionEventVariableSize* ev)
        {
            return (char*)((byte*)ev + InputEvent.kBaseEventSize + sizeof(int));
        }

        public FourCC typeStatic => Type;

        /// <summary>
        /// Returns composition string for the given event.
        /// </summary>
        /// <param name="ev">Pointer to the event.</param>
        /// <returns></returns>
        public static unsafe IMECompositionString GetIMECompositionString(IMECompositionEventVariableSize* ev)
        {
            return new IMECompositionString(GetCharsPtr(ev), ev->length);
        }

        /// <summary>
        /// Queues up an IME Composition Event. IME Event sizes are variable, and this simplifies the process of aligning up the Input Event information and actual IME composition string.
        /// </summary>
        /// <param name="deviceId">ID of the device (see <see cref="InputDevice.deviceId") to which the composition event should be sent to. Should be an <see cref="ITextInputReceiver"/> device. Will trigger <see cref="ITextInputReceiver.OnIMECompositionChanged"/> call when processed.</param>
        /// <param name="str">The IME characters to be sent. This can be any length, or left blank to represent a resetting of the IME dialog.</param>
        /// <param name="time">The time in seconds, the event was generated at.  This uses the same timeline as <see cref="Time.realtimeSinceStartup"/></param>
        public static unsafe void QueueEvent(int deviceId, string str, double time)
        {
            var sizeInBytes = (InputEvent.kBaseEventSize + sizeof(int)) + sizeof(char) * str.Length;
            var eventBuffer = new NativeArray<byte>(sizeInBytes, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var ev = (IMECompositionEventVariableSize*)eventBuffer.GetUnsafePtr();

            ev->baseEvent = new InputEvent(Type, sizeInBytes, deviceId, time);
            ev->length = str.Length;

            if (str.Length > 0)
                fixed(char* p = str)
                UnsafeUtility.MemCpy(GetCharsPtr(ev), p, str.Length * sizeof(char));

            InputSystem.QueueEvent(new InputEventPtr((InputEvent*)ev));

            eventBuffer.Dispose();
        }
    }

    //// TODO for v2 remove and replace with just string.
    /// <summary>
    /// A struct representing an string of characters generated by an IME for text input.
    /// </summary>
    /// <remarks>
    /// This is the internal representation of character strings in the event stream. It is exposed to user content through the
    /// <see cref="ITextInputReceiver.OnIMECompositionChanged"/> method. It can easily be converted to a normal C# string using
    ///  <see cref="ToString"/>, but is exposed as the raw struct to avoid allocating memory by default.
    /// </remarks>
    public unsafe struct IMECompositionString : IEnumerable<char>
    {
        private const int kLegacyIMEEventCharBufferSize = 64;

        private readonly string m_ManagedString;
        private readonly int m_Size;
        private fixed char m_FixedBuffer[kLegacyIMEEventCharBufferSize];

        private struct FixedBufferEnumerator : IEnumerator<char>
        {
            private IMECompositionString m_CompositionString;
            private int m_CurrentIndex;

            public FixedBufferEnumerator(IMECompositionString compositionString)
            {
                m_CompositionString = compositionString;
                m_CurrentIndex = -1;
            }

            public bool MoveNext()
            {
                if (m_CurrentIndex + 1 >= m_CompositionString.Count)
                    return false;

                m_CurrentIndex++;
                return true;
            }

            public void Reset()
            {
                m_CurrentIndex = -1;
            }

            public void Dispose()
            {
            }

            public char Current => m_CompositionString[m_CurrentIndex];

            object IEnumerator.Current => Current;
        }

        public int Count => m_Size;

        public char this[int index]
        {
            get
            {
                if (m_ManagedString != null)
                    return m_ManagedString[index];

                if (index >= Count || index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return m_FixedBuffer[index];
            }
        }

        public IMECompositionString(char* characters, int length)
        {
            // only allocate string if we can't fit into fixed buffer
            if (length <= kLegacyIMEEventCharBufferSize)
            {
                m_ManagedString = null;
                m_Size = length;
                if (m_Size > 0)
                {
                    Debug.Assert(characters != null);
                    fixed(char* dst = m_FixedBuffer)
                    UnsafeUtility.MemCpy(dst, characters, m_Size * sizeof(char));
                }
            }
            else
            {
                m_ManagedString = new string(characters, 0, length);
                m_Size = length;
            }
        }

        public IMECompositionString(string characters)
        {
            // string is already allocated on the heap, so reuse it
            m_ManagedString = characters;
            m_Size = characters.Length;
        }

        public override string ToString()
        {
            if (m_Size == 0)
                return string.Empty;

            if (m_ManagedString != null)
                return m_ManagedString;

            fixed(char* ptr = m_FixedBuffer)
            return new string(ptr, 0, m_Size);
        }

        public IEnumerator<char> GetEnumerator()
        {
            if (m_ManagedString != null)
                return m_ManagedString.GetEnumerator();

            return new FixedBufferEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
