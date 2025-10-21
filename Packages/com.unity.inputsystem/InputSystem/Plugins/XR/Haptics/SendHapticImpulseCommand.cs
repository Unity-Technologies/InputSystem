using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.XR.Haptics
{
    /// <summary>
    /// A device command sent to a device to set it's motor rumble amplitude for a set duration.
    /// </summary>
    /// <remarks>This is directly used by the <see cref="XRControllerWithRumble"/> class. For clearer details of using this command, see that class.</remarks>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct SendHapticImpulseCommand : IInputDeviceCommandInfo
    {
        static FourCC Type => new FourCC('X', 'H', 'I', '0');

        private const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(int) + (sizeof(float) * 2);

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        private int channel;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int))]
        private float amplitude;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int) + (sizeof(float)))]
        private float duration;

        public FourCC typeStatic => Type;

        /// <summary>
        /// Creates a device command that can then be sent to a specific device.
        /// </summary>
        /// <param name="motorChannel">The desired motor you want to rumble</param>
        /// <param name="motorAmplitude">The desired motor amplitude that should be within a [0-1] range.</param>
        /// <param name="motorDuration">The desired duration of the impulse in seconds.</param>
        /// <returns>The command that should be sent to the device via <c>InputDevice.ExecuteCommand</c>.</returns>
        public static SendHapticImpulseCommand Create(int motorChannel, float motorAmplitude, float motorDuration)
        {
            return new SendHapticImpulseCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                channel = motorChannel,
                amplitude = motorAmplitude,
                duration = motorDuration
            };
        }
    }
}
