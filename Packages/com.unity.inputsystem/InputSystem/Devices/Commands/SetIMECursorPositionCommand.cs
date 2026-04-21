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
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'P'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + (sizeof(float) * 2);

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>The screen position for the IME composition window.</summary>
        public Vector2 position
        {
            get { return m_Position; }
        }

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        Vector2 m_Position;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic
        {
            get { return Type; }
        }

        /// <summary>Creates a command to set the IME cursor position to the given screen coordinate.</summary>
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
