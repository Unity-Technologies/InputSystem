using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Sets the position for IME dialogs.  This is in pixels, from the upper left corner going down and to the right.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + (sizeof(float) * 2))]
    public unsafe struct SetIMECursorPositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'P'); } }

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        float xPosition;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(float))]
        float yPosition;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static SetIMECursorPositionCommand Create(Vector2 position)
        {
            return new SetIMECursorPositionCommand
            {
                baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.kBaseCommandSize + (sizeof(float) * 2)),
                xPosition = position.x,
                yPosition = position.y
            };
        }
    }
}
