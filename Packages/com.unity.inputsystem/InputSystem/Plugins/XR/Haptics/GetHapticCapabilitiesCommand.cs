using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    public struct HapticCapabilities
    {
        public HapticCapabilities(uint numChannels, uint frequencyHz, uint maxBufferSize)
        {
            this.numChannels = numChannels;
            this.frequencyHz = frequencyHz;
            this.maxBufferSize = maxBufferSize;
        }

        public uint numChannels { get; private set; }
        public uint frequencyHz { get; private set; }
        public uint maxBufferSize { get; private set; }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct GetHapticCapabilitiesCommand : IInputDeviceCommandInfo
    {
        static FourCC Type { get { return new FourCC('X', 'H', 'C', '0'); } }

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

        public HapticCapabilities capabilities
        {
            get
            {
                return new HapticCapabilities(numChannels, frequencyHz, maxBufferSize);
            }
        }

        public static GetHapticCapabilitiesCommand Create()
        {
            return new GetHapticCapabilitiesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
