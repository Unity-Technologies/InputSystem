using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct QuerySamplingFrequencyCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'M', 'P', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float frequency;

        public FourCC typeStatic
        {
            get { return Type; }
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
