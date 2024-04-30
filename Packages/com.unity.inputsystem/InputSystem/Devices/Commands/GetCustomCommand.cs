using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to get a custom value from the runtime
    /// For example, on Android, this command can be used to query whether the back button
    /// leaves the app or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct GetCustomCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('G', 'C', 'C'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(uint) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>
        /// Custom code to specify the type of action to perform at runtime.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint code;

        /// <summary>
        /// Payload associated with the custom code that will be retrieved from runtime.
        /// </summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(uint))]
        public uint payload;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static GetCustomCommand Create()
        {
            return new GetCustomCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}