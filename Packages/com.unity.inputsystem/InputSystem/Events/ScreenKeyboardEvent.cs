using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + ScreenKeyboardProperties.Size)]
    public struct ScreenKeyboardEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53434b45; // SCKE

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        public ScreenKeyboardProperties keyboardProperties;

        public FourCC typeStatic => Type;

        public static ScreenKeyboardEvent Create(int deviceId, ScreenKeyboardProperties keyboardProperties, double time = -1)
        {
            var inputEvent = new ScreenKeyboardEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + ScreenKeyboardProperties.Size, deviceId, time);
            inputEvent.keyboardProperties = keyboardProperties;
            return inputEvent;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = ScreenKeyboardProperties.Size)]
    public struct ScreenKeyboardProperties
    {
        public const int Size = sizeof(ScreenKeyboardState) + sizeof(float) * 4;

        [FieldOffset(0)]
        public ScreenKeyboardState State;

        [FieldOffset(4)]
        private Vector2 m_OccludingAreaPosition;

        [FieldOffset(12)]
        private Vector2 m_OccludingAreaSize;

        public Rect OccludingArea
        {
            set
            {
                m_OccludingAreaPosition = value.position;
                m_OccludingAreaSize = value.size;
            }
            get
            {
                return new Rect(m_OccludingAreaPosition, m_OccludingAreaSize);
            }
        }
    }
}


