using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct HapticCapabilities
    {
        public const int kSize = sizeof(uint) * 3;

        public HapticCapabilities(uint numChannels, uint frequencyHz, uint maxBufferSize)
        {
            this.numChannels = numChannels;
            this.frequencyHz = frequencyHz;
            this.maxBufferSize = maxBufferSize;
        }

        [FieldOffset(0)]
        public uint numChannels;

        [FieldOffset(sizeof(uint))]
        public uint frequencyHz;

        [FieldOffset((sizeof(uint) * 2))]
        public uint maxBufferSize;
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct HapticCapabilitiesCommand : IInputDeviceCommandInfo
    {
        static FourCC Type { get { return new FourCC('X', 'R', 'H', '0'); } }

        const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(uint) * 3;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint numChannels;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(uint))]
        public uint frequencyHz;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + (sizeof(uint) * 2))]
        public uint maxBufferSize;

        public HapticCapabilities Capabilities 
        {
            get
            {
                return new HapticCapabilities(numChannels, frequencyHz, maxBufferSize);
            }
        }

        public static HapticCapabilitiesCommand Create()
        {
            return new HapticCapabilitiesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
    /*
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct BufferedRumbleCommand : IInputDeviceCommandInfo
    {
        static FourCC Type { get { return new FourCC('X', 'R', 'U', '0'); } }

        const int kSize = InputDeviceCommand.kBaseCommandSize;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        /// <summary>
        /// Creates a device command that can then be sent to a specific device.
        /// </summary>
        /// <param name="motorIntensity">The desired motor intensity that should be within a [0-1] range.</param>
        /// <returns>The command that should be sent to the device via InputDevice.ExecuteCommand(InputDeviceCommand).  See SimpleXRRumble for more details.</returns>
        public static BufferedRumbleCommand Create()
        {
            return new BufferedRumbleCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
    */
}
