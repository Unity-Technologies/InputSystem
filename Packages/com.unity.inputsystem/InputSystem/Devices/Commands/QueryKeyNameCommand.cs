using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to query the current name of a key according to the current keyboard layout.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct QueryKeyNameCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('K', 'Y', 'C', 'F');

        internal const int kMaxNameLength = 256;
        internal const int kSize = InputDeviceCommand.kBaseCommandSize + kMaxNameLength + 4;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public int scanOrKeyCode;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public fixed byte nameBuffer[kMaxNameLength];

        public string ReadKeyName()
        {
            fixed(QueryKeyNameCommand * thisPtr = &this)
            {
                return StringHelpers.ReadStringFromBuffer(new IntPtr(thisPtr->nameBuffer), kMaxNameLength);
            }
        }

        public FourCC typeStatic => Type;

        public static QueryKeyNameCommand Create(Key key)
        {
            return new QueryKeyNameCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                scanOrKeyCode = (int)key
            };
        }
    }
}
