using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Command to query the current name of a key according to the current keyboard layout.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct QueryKeyNameCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('K', 'Y', 'C', 'F'); } }

        public const int kMaxNameLength = 256;
        public const int kSize = InputDeviceCommand.kBaseCommandSize + kMaxNameLength + 4;

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

        public FourCC GetTypeStatic()
        {
            return Type;
        }

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
