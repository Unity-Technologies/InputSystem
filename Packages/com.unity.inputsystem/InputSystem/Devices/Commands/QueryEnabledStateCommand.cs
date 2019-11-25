using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to find out whether a device is currently enabled or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryEnabledStateCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('Q', 'E', 'N', 'B');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool isEnabled;

        public FourCC typeStatic => Type;

        public static QueryEnabledStateCommand Create()
        {
            return new QueryEnabledStateCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
