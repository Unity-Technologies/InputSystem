using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct WarpMousePositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('W', 'P', 'M', 'S'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 warpPositionInPlayerDisplaySpace;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static WarpMousePositionCommand Create(Vector2 position)
        {
            return new WarpMousePositionCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                warpPositionInPlayerDisplaySpace = position
            };
        }
    }
}
