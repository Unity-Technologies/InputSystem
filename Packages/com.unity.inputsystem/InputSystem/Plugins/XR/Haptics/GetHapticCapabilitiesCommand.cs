// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.XR.Haptics
{
    /// <summary>
    /// Describes the haptic capabilities of a specific device.
    /// </summary>
    public struct HapticCapabilities
    {
        /// <summary>
        /// Initializes and returns an instance of <see cref="HapticCapabilities"/>.
        /// </summary>
        /// <param name="numChannels">The number of haptic channels available on this device.</param>
        /// <param name="supportsImpulse">This device supports sending a haptic impulse.</param>
        /// <param name="supportsBuffer">This device supports sending a haptic buffer.</param>
        /// <param name="frequencyHz">The buffer frequency the device operates at in Hertz.</param>
        /// <param name="maxBufferSize">The max amount of buffer data that can be stored by the device.</param>
        /// <param name="optimalBufferSize">The optimal size of a device's buffer, taking into account frequency and latency.</param>
        public HapticCapabilities(uint numChannels, bool supportsImpulse, bool supportsBuffer, uint frequencyHz, uint maxBufferSize, uint optimalBufferSize)
        {
            this.numChannels = numChannels;
            this.supportsImpulse = supportsImpulse;
            this.supportsBuffer = supportsBuffer;
            this.frequencyHz = frequencyHz;
            this.maxBufferSize = maxBufferSize;
            this.optimalBufferSize = optimalBufferSize;
        }

        /// <summary>
        /// Deprecated. Use <see cref="HapticCapabilities(uint, bool, bool, uint, uint, uint)"/> instead.
        /// This constructor did not match the native haptic capabilities struct and was missing properties.
        /// </summary>
        /// <param name="numChannels">The number of haptic channels available on this device.</param>
        /// <param name="frequencyHz">The buffer frequency the device operates at in Hertz.</param>
        /// <param name="maxBufferSize">The max amount of buffer data that can be stored by the device.</param>
        public HapticCapabilities(uint numChannels, uint frequencyHz, uint maxBufferSize)
            : this(numChannels, false, false, frequencyHz, maxBufferSize, 0U)
        {
        }

        /// <summary>
        /// The number of haptic channels available on this device.
        /// </summary>
        public uint numChannels { get; }

        /// <summary>
        /// This device supports sending a haptic impulse.
        /// </summary>
        /// <seealso cref="SendHapticImpulseCommand"/>
        public bool supportsImpulse { get; }

        /// <summary>
        /// This device supports sending a haptic buffer.
        /// </summary>
        /// <seealso cref="SendBufferedHapticCommand"/>
        public bool supportsBuffer { get; }

        /// <summary>
        /// The buffer frequency the device operates at in Hertz. This impacts how fast the device consumes buffered haptic data.
        /// </summary>
        /// <remarks>
        /// This value is greater than 0 if <see cref="supportsBuffer"/> is <see langword="true"/>, and 0 otherwise.
        /// </remarks>
        public uint frequencyHz { get; }

        /// <summary>
        /// The max amount of buffer data that can be stored by the device.
        /// </summary>
        public uint maxBufferSize { get; }

        /// <summary>
        /// The optimal size of a device's buffer, taking into account frequency and latency.
        /// </summary>
        public uint optimalBufferSize { get; }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct GetHapticCapabilitiesCommand : IInputDeviceCommandInfo
    {
        static FourCC Type => new FourCC('X', 'H', 'C', '0');

        // 20 bytes of data from uint(4) + bool(1) + bool(1) + padding + uint(4) + uint(4) + uint(4)
        const int kSize = InputDeviceCommand.kBaseCommandSize + 20;

        public FourCC typeStatic => Type;

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint numChannels;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public bool supportsImpulse;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 5)]
        public bool supportsBuffer;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)]
        public uint frequencyHz;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 12)]
        public uint maxBufferSize;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 16)]
        public uint optimalBufferSize;

        public HapticCapabilities capabilities => new HapticCapabilities(numChannels, supportsImpulse, supportsBuffer, frequencyHz, maxBufferSize, optimalBufferSize);

        public static GetHapticCapabilitiesCommand Create()
        {
            return new GetHapticCapabilitiesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
#endif
