using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Command to re-enable a device that has been disabled with <see cref="DisableDeviceCommand"/>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct EnableDeviceCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('E', 'N', 'B', 'L'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic()
        {
            return Type;
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
