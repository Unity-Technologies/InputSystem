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
        public static FourCC Type { get { return new FourCC('E', 'N', 'B', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static EnableDeviceCommand Create()
        {
            return new EnableDeviceCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
