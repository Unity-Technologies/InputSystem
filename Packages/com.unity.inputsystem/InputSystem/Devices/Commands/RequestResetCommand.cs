using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A command to tell the runtime to reset the device to it's default state.
    /// </summary>
    /// <remarks>
    /// This triggers an event being sent from the device that represents an empty, or untouched device.
    /// </remarks>
    /// <seealso cref="RequestSyncCommand"/>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize)]
    public struct RequestResetCommand : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type => new FourCC('R', 'S', 'E', 'T');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Creates a request reset command.</summary>
        public static RequestResetCommand Create()
        {
            return new RequestResetCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
