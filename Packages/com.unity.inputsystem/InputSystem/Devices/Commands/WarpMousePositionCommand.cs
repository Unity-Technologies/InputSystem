using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct WarpMousePositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('W', 'P', 'M', 'S'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 warpPositionInPlayerDisplaySpace;

        public FourCC typeStatic
        {
            get { return Type; }
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
