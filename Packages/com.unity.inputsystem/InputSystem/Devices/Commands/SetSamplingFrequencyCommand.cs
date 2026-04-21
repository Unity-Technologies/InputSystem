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
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type { get { return new FourCC('S', 'S', 'P', 'L'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float);

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>The desired sampling frequency in Hz.</summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float frequency;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic
        {
            get { return Type; }
        }

        /// <summary>Creates a command to set the device's sampling frequency.</summary>
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
