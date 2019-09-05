using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Queries to see if this device is able to continue to send updates and state changes when the application is not if focus.
    /// </summary>
    /// <seealso cref="InputDevice.canRunInBackground"/>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(bool))]
    public struct QueryCanRunInBackground : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('Q', 'R', 'I', 'B');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool canRunInBackground;

        public FourCC typeStatic => Type;

        public static QueryCanRunInBackground Create()
        {
            return new QueryCanRunInBackground
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                canRunInBackground = false
            };
        }
    }
}
