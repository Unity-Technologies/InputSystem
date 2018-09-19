using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Sets the position for IME dialogs.  This is in pixels, from the upper left corner going down and to the right.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct SetIMECursorPositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'P'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + (sizeof(float) * 2);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        Vector2 position;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static SetIMECursorPositionCommand Create(Vector2 cursorPosition)
        {
            return new SetIMECursorPositionCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                position = cursorPosition
            };
        }
    }
}
