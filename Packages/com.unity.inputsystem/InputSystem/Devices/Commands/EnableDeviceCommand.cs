using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to re-enable a device that has been disabled with <see cref="DisableDeviceCommand"/>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct EnableDeviceCommand : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type { get { return new FourCC('E', 'N', 'B', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic
        {
            get { return Type; }
        }

        /// <summary>Creates an enable device command.</summary>
        public static EnableDeviceCommand Create()
        {
            return new EnableDeviceCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
