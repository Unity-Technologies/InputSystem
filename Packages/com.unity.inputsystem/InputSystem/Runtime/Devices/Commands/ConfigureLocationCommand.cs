using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to set the desired accuracy and update-distance threshold of a <see cref="LocationSensor"/>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct ConfigureLocationCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('L', 'C', 'F', 'G');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float desiredAccuracyInMeters;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(float))]
        public float updateDistanceInMeters;

        public FourCC typeStatic => Type;

        public static ConfigureLocationCommand Create(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            return new ConfigureLocationCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                desiredAccuracyInMeters = desiredAccuracyInMeters,
                updateDistanceInMeters = updateDistanceInMeters
            };
        }
    }
}
