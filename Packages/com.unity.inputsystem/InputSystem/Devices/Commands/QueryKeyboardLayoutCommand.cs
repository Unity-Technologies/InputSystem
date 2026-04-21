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
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type { get { return new FourCC('K', 'B', 'L', 'T'); } }

        internal const int kMaxNameLength = 256;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Output buffer filled in with the keyboard layout name after the command executes.</summary>
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

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Creates a query keyboard layout command.</summary>
        public static QueryKeyboardLayoutCommand Create()
        {
            return new QueryKeyboardLayoutCommand
            {
                baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.kBaseCommandSize + kMaxNameLength)
            };
        }
    }
}
