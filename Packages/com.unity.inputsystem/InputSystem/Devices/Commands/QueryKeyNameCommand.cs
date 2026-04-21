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
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type => new FourCC('K', 'Y', 'C', 'F');

        internal const int kMaxNameLength = 256;
        internal const int kSize = InputDeviceCommand.kBaseCommandSize + kMaxNameLength + 4;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>The scan code or key code to query the name for.</summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public int scanOrKeyCode;

        /// <summary>Output buffer filled in with the key name after the command executes.</summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public fixed byte nameBuffer[kMaxNameLength];

        /// <summary>Reads the key name from the output buffer as a string.</summary>
        public string ReadKeyName()
        {
            fixed(QueryKeyNameCommand * thisPtr = &this)
            {
                return StringHelpers.ReadStringFromBuffer(new IntPtr(thisPtr->nameBuffer), kMaxNameLength);
            }
        }

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Creates a query key name command for the given key.</summary>
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
