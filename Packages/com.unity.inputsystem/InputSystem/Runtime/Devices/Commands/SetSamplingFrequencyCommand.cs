using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

////REVIEW: switch this to interval-in-seconds instead of Hz?

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// For a device that is sampled periodically, set the frequency at which the device
    /// is sampled.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct SetSamplingFrequencyCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'S', 'P', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float frequency;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static SetSamplingFrequencyCommand Create(float frequency)
        {
            return new SetSamplingFrequencyCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                frequency = frequency
            };
        }
    }
}
