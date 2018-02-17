using System.Runtime.InteropServices;
using ISX.Utilities;
using UnityEngine;

namespace ISX.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryEditorWindowCoordinates : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('E', 'W', 'P', 'S'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 inOutCoordinates;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static QueryEditorWindowCoordinates Create(Vector2 playerWindowCoordinates)
        {
            return new QueryEditorWindowCoordinates
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                inOutCoordinates = playerWindowCoordinates
            };
        }
    }
}
