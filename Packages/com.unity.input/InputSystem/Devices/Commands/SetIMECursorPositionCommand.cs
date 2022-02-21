using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Sets the position for IME dialogs.  This is in pixels, from the upper left corner going down and to the right.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct SetIMECursorPositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'P'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + (sizeof(float) * 2);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public Vector2 position
        {
            get { return m_Position; }
        }

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        Vector2 m_Position;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static SetIMECursorPositionCommand Create(Vector2 cursorPosition)
        {
            return new SetIMECursorPositionCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                m_Position = cursorPosition
            };
        }
    }
}
