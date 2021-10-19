#if UNITY_XR_AVAILABLE && ENABLE_VR || PACKAGE_DOCS_GENERATION && !UNITY_FORCE_INPUTSYSTEM_XR_OFF
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.XR.Haptics
{
    /// <summary>
    /// A device command sent to a device to set it's motor rumble amplitude for a set duration.
    /// </summary>
    /// <remarks>This is directly used by the SimpleXRRumble class.  For clearer details of using this command, see that class.</remarks>
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
        /// <returns>The command that should be sent to the device via InputDevice.ExecuteCommand(InputDeviceCommand).  See XRHaptics for more details.</returns>
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
#endif // UNITY_XR_AVAILABLE  || PACKAGE_DOCS_GENERATION
