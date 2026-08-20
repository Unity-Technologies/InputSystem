#if UNITY_INPUTSYSTEM_SUPPORTS_CAPABILITY_QUERIES
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Queries whether the platform can deliver mouse input at all, as opposed to whether a mouse is
    /// connected right now.
    /// </summary>
    /// <remarks>
    /// Addressed to the engine's system endpoint rather than to a device, so it is sent through
    /// <see cref="InputManager.ExecuteSystemCommand{TCommand}"/> rather than
    /// <see cref="InputDevice.ExecuteCommand{TCommand}"/>.
    ///
    /// The FourCC must match <c>kInputFourCCIOCTLQueryMouseSupported</c> in the engine's
    /// input module.
    /// </remarks>
    /// <seealso cref="Mouse.isSupported"/>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct QueryMouseSupportedCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('Q', 'M', 'O', 'U');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(byte);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public InputCapabilitySupport isSupported;

        public FourCC typeStatic => Type;

        public static QueryMouseSupportedCommand Create()
        {
            return new QueryMouseSupportedCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                isSupported = InputCapabilitySupport.Unknown
            };
        }
    }
}
#endif // UNITY_INPUTSYSTEM_SUPPORTS_CAPABILITY_QUERIES
