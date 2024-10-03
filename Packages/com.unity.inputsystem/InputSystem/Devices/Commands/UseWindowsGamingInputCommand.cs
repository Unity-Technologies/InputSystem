using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    // Command to enable or disable Windows.Gaming.Input native backend.
    // Send it to deviceId 0 as it's a special "global" IOCTL that gets routed internally.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct UseWindowsGamingInputCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('U', 'W', 'G', 'I'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(byte);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public byte enable;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static UseWindowsGamingInputCommand Create(bool enable)
        {
            return new UseWindowsGamingInputCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                enable = (byte)(enable ? 1 : 0)
            };
        }
    }
}
