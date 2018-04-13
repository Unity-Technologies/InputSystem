using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct QueryUserIdCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('U', 'S', 'E', 'R'); } }

        public const int kMaxIdLength = 256;
        public const int kSize = InputDeviceCommand.kBaseCommandSize + kMaxIdLength + 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public fixed byte idBuffer[kMaxIdLength];

        public string ReadId()
        {
            fixed(QueryUserIdCommand * thisPtr = &this)
            {
                return StringHelpers.ReadStringFromBuffer(new IntPtr(thisPtr->idBuffer), kMaxIdLength);
            }
        }

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static QueryUserIdCommand Create()
        {
            return new QueryUserIdCommand()
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
