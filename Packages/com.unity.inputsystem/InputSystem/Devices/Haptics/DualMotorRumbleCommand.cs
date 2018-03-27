using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct DualMotorRumbleCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('R', 'M', 'B', 'L'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

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

        public static DualMotorRumbleCommand Create(float lowFrequency, float highFrequency)
        {
            return new DualMotorRumbleCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                lowFrequencyMotorSpeed = lowFrequency,
                highFrequencyMotorSpeed = highFrequency
            };
        }
    }
}
