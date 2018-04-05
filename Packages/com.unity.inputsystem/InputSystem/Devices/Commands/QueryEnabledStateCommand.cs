using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    /// <summary>
    /// Command to find out whether a device is currently enabled or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryEnabledStateCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('Q', 'E', 'N', 'B'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool isEnabled;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static QueryEnabledStateCommand Create()
        {
            return new QueryEnabledStateCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
