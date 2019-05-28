using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Query dimensions of a device.
    /// </summary>
    /// <remarks>
    /// This is usually used to query screen dimensions from pointer devices.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryDimensionsCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('D', 'I', 'M', 'S'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 outDimensions;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static QueryDimensionsCommand Create()
        {
            return new QueryDimensionsCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
