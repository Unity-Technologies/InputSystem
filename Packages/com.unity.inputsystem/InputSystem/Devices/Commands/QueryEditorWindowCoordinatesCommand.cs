#if UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryEditorWindowCoordinatesCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('E', 'W', 'P', 'S'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 inOutCoordinates;

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static QueryEditorWindowCoordinatesCommand Create(Vector2 playerWindowCoordinates)
        {
            return new QueryEditorWindowCoordinatesCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                inOutCoordinates = playerWindowCoordinates
            };
        }
    }
}
#endif // UNITY_EDITOR
