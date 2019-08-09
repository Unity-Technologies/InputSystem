#if UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

////REVIEW: This mechanism sucks. We should have this conversion without the device having to support it through an IOCTL. A Pointer
////        should just inherently have this conversion mechanism on its controls that operate in screen space.

namespace UnityEngine.InputSystem.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct QueryEditorWindowCoordinatesCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('E', 'W', 'P', 'S');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 inOutCoordinates;

        public FourCC typeStatic => Type;

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
