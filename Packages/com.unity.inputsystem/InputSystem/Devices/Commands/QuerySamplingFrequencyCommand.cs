using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QuerySamplingFrequencyCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'M', 'P', 'L'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float frequency;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static QuerySamplingFrequencyCommand Create()
        {
            return new QuerySamplingFrequencyCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
