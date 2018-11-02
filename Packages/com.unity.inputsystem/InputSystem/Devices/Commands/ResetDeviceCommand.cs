using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(bool))]
    public unsafe struct ResetDeviceCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('R', 'S', 'E', 'T'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool needsManagedReset;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static ResetDeviceCommand Create()
        {
            return new ResetDeviceCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                needsManagedReset = false
            };
        }
    }
}
