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

    /// <summary>
    /// Input device command struct for retrieving the haptic capabilities of a device.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct GetHapticCapabilitiesCommand : IInputDeviceCommandInfo
    {
        static FourCC Type => new FourCC('X', 'H', 'C', '0');

        // 20 bytes of data from uint(4) + bool(1) + bool(1) + padding + uint(4) + uint(4) + uint(4)
        const int kSize = InputDeviceCommand.kBaseCommandSize + 20;

        /// <inheritdoc />
        public FourCC typeStatic => Type;

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        /// <summary>
        /// The number of haptic channels available on this device.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint numChannels;

        /// <summary>
        /// This device supports sending a haptic impulse.
        /// </summary>
        /// <seealso cref="SendHapticImpulseCommand"/>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 4)]
        public bool supportsImpulse;

        /// <summary>
        /// This device supports sending a haptic buffer.
        /// </summary>
        /// <seealso cref="SendBufferedHapticCommand"/>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 5)]
        public bool supportsBuffer;

        /// <summary>
        /// The buffer frequency the device operates at in Hertz. This impacts how fast the device consumes buffered haptic data.
        /// </summary>
        /// <remarks>
        /// This value is greater than 0 if <see cref="supportsBuffer"/> is <see langword="true"/>, and 0 otherwise.
        /// </remarks>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)]
        public uint frequencyHz;

        /// <summary>
        /// The max amount of buffer data that can be stored by the device.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 12)]
        public uint maxBufferSize;

        /// <summary>
        /// The optimal size of a device's buffer, taking into account frequency and latency.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 16)]
        public uint optimalBufferSize;

        /// <summary>
        /// The haptic capabilities of the device, populated after this command is executed.
        /// </summary>
        public HapticCapabilities capabilities => new HapticCapabilities(numChannels, supportsImpulse, supportsBuffer, frequencyHz, maxBufferSize, optimalBufferSize);

        /// <summary>
        /// Creates and returns a new initialized input device command struct for retrieving
        /// the haptic capabilities of a device when executed.
        /// </summary>
        /// <returns>Returns a new command struct with the data header initialized, making it ready to execute.</returns>
        /// <seealso cref="InputDevice.ExecuteCommand{TCommand}(ref TCommand)"/>
        public static GetHapticCapabilitiesCommand Create()
        {
            return new GetHapticCapabilitiesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
