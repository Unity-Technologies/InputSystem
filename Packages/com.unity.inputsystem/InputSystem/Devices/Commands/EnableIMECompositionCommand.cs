using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Device Command that enables IME Composition within the application.  Primarily handled by Keyboard devices.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(byte))]
    public unsafe struct EnableIMECompositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'M'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + +sizeof(uint);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>
        /// Set to true, and if true, Input Method Editors will be used while typing.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        byte imeEnabled;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static EnableIMECompositionCommand Create(bool enabled)
        {
            return new EnableIMECompositionCommand
            {
                baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.kBaseCommandSize + sizeof(byte)),
                imeEnabled = enabled ? byte.MaxValue : (byte)0
            };
        }
    }
}
