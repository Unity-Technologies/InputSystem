using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(uint))]
    public unsafe struct SetIMECompositionModeCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'M'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + +sizeof(uint);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        uint compositionMode;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static SetIMECompositionModeCommand Create(IMECompositionMode mode)
        {
            return new SetIMECompositionModeCommand
            {
                baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.kBaseCommandSize + sizeof(uint)),
                compositionMode = (uint)mode
            };
        }
    }
}
