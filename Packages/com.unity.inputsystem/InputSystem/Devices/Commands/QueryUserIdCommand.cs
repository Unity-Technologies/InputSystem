using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

////TODO: remove this one; superseded by QueryPairedUserAccountCommand

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal unsafe struct QueryUserIdCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('U', 'S', 'E', 'R'); } }

        public const int kMaxIdLength = 256;
        internal const int kSize = InputDeviceCommand.kBaseCommandSize + kMaxIdLength * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public fixed byte idBuffer[kMaxIdLength * 2];

        public string ReadId()
        {
            fixed(QueryUserIdCommand * thisPtr = &this)
            {
                return StringHelpers.ReadStringFromBuffer(new IntPtr(thisPtr->idBuffer), kMaxIdLength);
            }
        }

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static QueryUserIdCommand Create()
        {
            return new QueryUserIdCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
