using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(bool))]
    internal unsafe struct QueryCanRunInBackground : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('Q', 'R', 'I', 'B'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool canRunInBackground;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static QueryCanRunInBackground Create()
        {
            return new QueryCanRunInBackground
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                canRunInBackground = false
            };
        }
    }
}
