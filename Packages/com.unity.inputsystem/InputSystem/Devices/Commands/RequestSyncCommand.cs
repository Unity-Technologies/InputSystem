using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A command to tell the runtime to sync the device to it's last known state.
    /// </summary>
    /// <remarks>
    /// This triggers an event from the underlying device that represents the whole, current state.
    /// </remarks>
    /// <seealso cref="RequestResetCommand"/>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize)]
    public unsafe struct RequestSyncCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'Y', 'N', 'C'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static RequestSyncCommand Create()
        {
            return new RequestSyncCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
