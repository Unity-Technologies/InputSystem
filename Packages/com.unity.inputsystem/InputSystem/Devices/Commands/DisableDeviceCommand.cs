using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Command to tell the runtime to no longer send events for the given device.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct DisableDeviceCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('D', 'S', 'B', 'L'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static DisableDeviceCommand Create()
        {
            return new DisableDeviceCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
