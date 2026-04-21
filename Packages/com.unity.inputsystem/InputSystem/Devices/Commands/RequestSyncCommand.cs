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
    public struct RequestSyncCommand : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type => new FourCC('S', 'Y', 'N', 'C');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Creates a request sync command.</summary>
        public static RequestSyncCommand Create()
        {
            return new RequestSyncCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
