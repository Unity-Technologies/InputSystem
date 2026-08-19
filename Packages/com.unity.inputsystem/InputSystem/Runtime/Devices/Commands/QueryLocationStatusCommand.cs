using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to query the current status of a <see cref="LocationSensor"/>'s underlying platform location service.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct QueryLocationStatusCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('L', 'S', 'T', 'A');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(int);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        // 0 Stopped, 1 Initializing, 2 Running, 3 Failed (matches LocationServiceStatus).
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public int status;

        public FourCC typeStatic => Type;

        public static QueryLocationStatusCommand Create()
        {
            return new QueryLocationStatusCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
