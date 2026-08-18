#if UNITY_INPUTSYSTEM_SUPPORTS_CAPABILITY_QUERIES
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Queries whether the platform delivers a real pressure value with touch input, as opposed to
    /// the constant 1.0 reported by platforms that have touch but no pressure.
    /// </summary>
    /// <remarks>
    /// Addressed to the engine's system endpoint rather than to a device, so it is sent through
    /// <see cref="InputManager.ExecuteSystemCommand{TCommand}"/> rather than
    /// <see cref="InputDevice.ExecuteCommand{TCommand}"/>.
    ///
    /// This is answered at platform scope rather than per touchscreen, because that is the scope at
    /// which the answer exists: every platform sources it from a device-model or OS-API property
    /// rather than by enumerating digitizers.
    ///
    /// The FourCC must match <c>kInputFourCCIOCTLQueryTouchPressureSupported</c> in the engine's
    /// <c>Modules/Input/InputFourCC.h</c>.
    /// </remarks>
    /// <seealso cref="Touchscreen.isPressureSupported"/>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct QueryTouchPressureSupportedCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('Q', 'T', 'P', 'S');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(byte);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public InputCapabilitySupport isSupported;

        public FourCC typeStatic => Type;

        public static QueryTouchPressureSupportedCommand Create()
        {
            return new QueryTouchPressureSupportedCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                isSupported = InputCapabilitySupport.Unknown
            };
        }
    }
}
#endif // UNITY_INPUTSYSTEM_SUPPORTS_CAPABILITY_QUERIES
