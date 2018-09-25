using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A specialized event that contains the current IME Composition string, if IME is enabled and active.
    /// This event contains the entire current string to date, and once a new composition is submitted will send a blank string event.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + sizeof(int) + (sizeof(char) * kIMECharBufferSize))]
    public unsafe struct IMECompositionEvent : IInputEventTypeInfo
    {
        // These needs to match the native ImeCompositionStringInputEventData settings
        public const int kIMECharBufferSize = 64;
        public const int Type = 0x494D4553;

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        public IMEComposition composition;

        public FourCC GetTypeStatic()
        {
            return Type;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    [StructLayout(LayoutKind.Explicit, Size = sizeof(int) + (sizeof(char) * LowLevel.IMECompositionEvent.kIMECharBufferSize))]
    public unsafe struct IMEComposition : IEnumerable<char>
    {
        internal unsafe struct Enumerator : IEnumerator<char>
        {
            IMEComposition m_Composition;
            char m_CurrentCharacter;
            int m_CurrentIndex;

            public Enumerator(IMEComposition composition)
            {
                m_Composition = composition;
                m_CurrentCharacter = '\0';
                m_CurrentIndex = -1;
            }

            public bool MoveNext()
            {
                int size = m_Composition.Count;

                m_CurrentIndex++;

                if (m_CurrentIndex == size)
                    return false;

                fixed (char* ptr = m_Composition.buffer)
                {
                    m_CurrentCharacter = *(ptr + m_CurrentIndex);
                }

                return true;
            }

            public void Reset()
            {
                m_CurrentIndex = -1;
            }

            public void Dispose()
            {
            }

            public char Current
            {
                get
                {
                    return m_CurrentCharacter;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        public int Count
        {
            get
            {
                return size;
            }
        }

        public unsafe char this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                    throw new IndexOutOfRangeException();

                fixed(char* ptr = buffer)
                {
                    return *(ptr + index);
                }
            }
        }

        [FieldOffset(0)]
        int size;

        [FieldOffset(sizeof(int))]
        fixed char buffer[LowLevel.IMECompositionEvent.kIMECharBufferSize];

        public override string ToString()
        {
            fixed(char* ptr = buffer)
            return new string(ptr);
        }

        public IEnumerator<char> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
