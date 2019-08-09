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
        public static FourCC Type { get { return new FourCC('D', 'S', 'B', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC typeStatic
        {
            get { return Type; }
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
