using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
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

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 outDimensions;

        public FourCC GetTypeStatic()
        {
            return Type;
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
