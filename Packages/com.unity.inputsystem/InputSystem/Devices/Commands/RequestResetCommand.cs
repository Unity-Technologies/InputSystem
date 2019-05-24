using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A command to tell the runtime to reset the device to it's default state.
    /// </summary>
    /// <remarks>
    /// This triggers an event being sent from the device that represents an empty, or untouched device
    /// </remarks>
    /// <seealso cref="RequestSyncCommand"/>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize)]
    public unsafe struct RequestResetCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('R', 'S', 'E', 'T'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC typeStatic
        {
            get { return Type; }
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
