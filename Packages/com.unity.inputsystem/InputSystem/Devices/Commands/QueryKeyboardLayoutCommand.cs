using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to query the name of the current keyboard layout from a device.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + kMaxNameLength)]
    public unsafe struct QueryKeyboardLayoutCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('K', 'B', 'L', 'T'); } }

        internal const int kMaxNameLength = 256;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public fixed byte nameBuffer[kMaxNameLength];

        /// <summary>
        /// Read the current keyboard layout name from <see cref="nameBuffer"/>.
        /// </summary>
        /// <returns></returns>
        public string ReadLayoutName()
        {
            fixed(QueryKeyboardLayoutCommand * thisPtr = &this)
            return StringHelpers.ReadStringFromBuffer(new IntPtr(thisPtr->nameBuffer), kMaxNameLength);
        }

        /// <summary>
        /// Write the given string to <see cref="nameBuffer"/>.
        /// </summary>
        /// <param name="name">Keyboard layout name.</param>
        public void WriteLayoutName(string name)
        {
            fixed(QueryKeyboardLayoutCommand * thisPtr = &this)
            StringHelpers.WriteStringToBuffer(name, new IntPtr(thisPtr->nameBuffer), kMaxNameLength);
        }

        public FourCC typeStatic => Type;

        public static QueryKeyboardLayoutCommand Create()
        {
            return new QueryKeyboardLayoutCommand
            {
                baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.kBaseCommandSize + kMaxNameLength)
            };
        }
    }
}
