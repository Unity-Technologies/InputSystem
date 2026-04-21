using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to tell the runtime to no longer send events for the given device.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct DisableDeviceCommand : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type { get { return new FourCC('D', 'S', 'B', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic
        {
            get { return Type; }
        }

        /// <summary>Creates a disable device command.</summary>
        public static DisableDeviceCommand Create()
        {
            return new DisableDeviceCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
