using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    /// <summary>
    /// A device command sent to a device to set it's motor rumble intensity.
    /// </summary>
    /// <remarks>This is directly used by the SimpleXRRumble class.  For clearer details of using this command, see that class.</remarks>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct SendSimpleRumbleCommand : IInputDeviceCommandInfo
    {
        static FourCC Type { get { return new FourCC('X', 'R', 'R', '0'); } }

        const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float);

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        float intensity;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        /// <summary>
        /// Creates a device command that can then be sent to a specific device.
        /// </summary>
        /// <param name="motorIntensity">The desired motor intensity that should be within a [0-1] range.</param>
        /// <returns>The command that should be sent to the device via InputDevice.ExecuteCommand(InputDeviceCommand).  See SimpleXRRumble for more details.</returns>
        public static SendSimpleRumbleCommand Create(float motorIntensity)
        {
            return new SendSimpleRumbleCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                intensity = motorIntensity
            };
        }
    }
}
