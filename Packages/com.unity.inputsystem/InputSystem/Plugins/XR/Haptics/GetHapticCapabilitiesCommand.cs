#if UNITY_INPUT_SYSTEM_ENABLE_XR || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.XR.Haptics
{
    public struct HapticCapabilities
    {
        public HapticCapabilities(int numChannels, bool supportsImpulse)
        {
            this.numChannels = numChannels;
            this.supportsImpulse = supportsImpulse;
        }

        public int numChannels { get; private set; }
        public bool supportsImpulse { get; private set; }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct GetHapticCapabilitiesCommand : IInputDeviceCommandInfo
    {
        static FourCC Type => new FourCC('X', 'H', 'C', '0');

        const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(int) + (sizeof(bool) * 2) + (sizeof(uint) * 3);

        public FourCC typeStatic => Type;

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        private int numChannels;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int))]
        private bool supportsImpulse;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int) + sizeof(bool))]
        private bool supportsBuffer;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int) + (sizeof(bool) * 2))]
        private uint frequencyHz;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int) + (sizeof(bool) * 2) + (sizeof(uint)))]
        private uint maxBufferSize;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int) + (sizeof(bool) * 2) + (sizeof(uint) * 2))]
        private uint optimalBufferSize;

        public HapticCapabilities capabilities => new HapticCapabilities(numChannels, supportsImpulse);

        public static GetHapticCapabilitiesCommand Create()
        {
            return new GetHapticCapabilitiesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
#endif // UNITY_INPUT_SYSTEM_ENABLE_XR
