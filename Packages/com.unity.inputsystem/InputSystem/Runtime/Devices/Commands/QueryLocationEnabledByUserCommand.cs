using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Command to query whether the user has granted OS-level permission for a <see cref="LocationSensor"/> to access location data.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct QueryLocationEnabledByUserCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('L', 'U', 'S', 'R');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool enabledByUser;

        public FourCC typeStatic => Type;

        public static QueryLocationEnabledByUserCommand Create()
        {
            return new QueryLocationEnabledByUserCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
