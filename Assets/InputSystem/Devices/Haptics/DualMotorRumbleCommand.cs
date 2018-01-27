using System.Runtime.InteropServices;
using ISX.Utilities;

namespace ISX.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + 8)]
    public struct DualMotorRumbleCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('R', 'M', 'B', 'L'); } }

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float lowFrequencyMotorSpeed;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public float highFrequencyMotorSpeed;

        public FourCC GetTypeStatic()
        {
            return Type;
        }
    }
}
