using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to set a custom value in the runtime
    /// For example, on Android, this command can set wether the back button
    /// leaves the app or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct SetCustomCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'C', 'C'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(uint) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>
        /// Custom code to specify the type of action to perform at runtime.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint code;

        /// <summary>
        /// Payload associated with the custom code that will be set in runtime.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(uint))]
        public uint payload;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static SetCustomCommand Create(uint code, uint payload)
        {
            return new SetCustomCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                code = code,
                payload = payload,
            };
        }
    }
}
