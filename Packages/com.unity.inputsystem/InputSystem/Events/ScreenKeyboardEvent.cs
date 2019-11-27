using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputEvent.kBaseEventSize + ScreenKeyboardState.Size)]
    public struct ScreenKeyboardEvent : IInputEventTypeInfo
    {
        public const int Type = 0x53434b45; // SCKE

        [FieldOffset(0)]
        public InputEvent baseEvent;

        [FieldOffset(InputEvent.kBaseEventSize)]
        public ScreenKeyboardState state;

        public FourCC typeStatic => Type;

        public static ScreenKeyboardEvent Create(int deviceId, ScreenKeyboardState state, double time = -1)
        {
            var inputEvent = new ScreenKeyboardEvent();
            inputEvent.baseEvent = new InputEvent(Type, InputEvent.kBaseEventSize + ScreenKeyboardState.Size, deviceId, time);
            inputEvent.state = state;
            return inputEvent;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = ScreenKeyboardState.Size)]
    public struct ScreenKeyboardState
    {
        public const int Size = sizeof(ScreenKeyboardStatus) + sizeof(float) * 4;

        [FieldOffset(0)]
        public ScreenKeyboardStatus Status;

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


