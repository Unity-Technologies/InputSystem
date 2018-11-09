using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// A command to tell the runtime to reset the device to it's default or last known state.
    /// </summary>
    /// <remarks>
    /// This triggers an event being sent from the device, and depending on the underlying implementation of the device can either be a clear, unused device, or the current state, depending on what's available to that backend.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(bool))]
    public unsafe struct RequestResetCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('R', 'S', 'E', 'T'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static RequestResetCommand Create()
        {
            return new RequestResetCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
