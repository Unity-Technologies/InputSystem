#if ENABLE_VR || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.XR.Haptics
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
        static FourCC Type => new FourCC('X', 'H', 'C', '0');

        const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(uint) * 3;

        public FourCC typeStatic => Type;

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint numChannels;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(uint))]
        public uint frequencyHz;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + (sizeof(uint) * 2))]
        public uint maxBufferSize;

        public HapticCapabilities capabilities => new HapticCapabilities(numChannels, frequencyHz, maxBufferSize);

        public static GetHapticCapabilitiesCommand Create()
        {
            return new GetHapticCapabilitiesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
#endif // ENABLE_VR
