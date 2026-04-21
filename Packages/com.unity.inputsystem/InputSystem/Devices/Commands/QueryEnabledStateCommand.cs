using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to find out whether a device is currently enabled or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryEnabledStateCommand : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type => new FourCC('Q', 'E', 'N', 'B');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Output field indicating whether the device is enabled after the command executes.</summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool isEnabled;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Creates a query enabled state command.</summary>
        public static QueryEnabledStateCommand Create()
        {
            return new QueryEnabledStateCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
