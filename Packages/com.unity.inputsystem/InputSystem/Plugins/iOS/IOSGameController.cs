#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;
using ISX.Plugins.iOS.LowLevel;
using ISX.Utilities;

namespace ISX.Plugins.iOS.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IOSGameControllerState : IInputStateTypeInfo
    {
        enum Button
        {
            DpadUp,
            DpadDown,
            DpadLeft,
            DpadRight,
            LeftStick,
            RightStick,
            LeftShoulder,
            RightShoulder,
            LeftTrigger,
            RightTrigger,
            X,
            Y,
            A,
            B
        };

        enum Axis
        {
            LeftStickX,
            LeftStickY,
            RightStickX,
            RightStickY
        };

        public static FourCC kFormat = new FourCC('I', 'G', 'C', ' ');
        public const int kMaxButtons = 14;
        public const int kMaxAxis = 4;

        [InputControl(name = "dpad")]
        [InputControl(name = "dpad/up", bit = (uint)Button.DpadUp)]
        [InputControl(name = "dpad/right", bit = (uint)Button.DpadRight)]
        [InputControl(name = "dpad/down", bit = (uint)Button.DpadDown)]
        [InputControl(name = "dpad/left", bit = (uint)Button.DpadLeft)]
        [InputControl(name = "buttonSouth", bit = (uint)Button.A)]
        [InputControl(name = "buttonWest", bit = (uint)Button.X)]
        [InputControl(name = "buttonNorth", bit = (uint)Button.Y)]
        [InputControl(name = "buttonEast", bit = (uint)Button.B)]
        [InputControl(name = "leftStickPress", bit = (uint)Button.LeftStick)]
        [InputControl(name = "rightStickPress", bit = (uint)Button.RightStick)]
        [InputControl(name = "leftShoulder", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", bit = (uint)Button.RightShoulder)]
        public uint buttons;
        
        [InputControl(name = "leftTrigger", offset = sizeof(uint) + sizeof(float) * (uint)Button.LeftTrigger)]
        [InputControl(name = "rightTrigger", offset = sizeof(uint) + sizeof(float) * (uint)Button.RightTrigger)]
        public fixed float buttonValues[kMaxButtons];

        private const uint kAxisOffset = sizeof(uint) + sizeof(float) * kMaxButtons;
        [InputControl(name = "leftStick", offset = (uint)Axis.LeftStickX * sizeof(float) + kAxisOffset)]
        [InputControl(name = "rightStick", offset = (uint)Axis.RightStickX * sizeof(float) + kAxisOffset)]
        public fixed float axisValues[kMaxAxis];

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace ISX.Plugins.iOS
{
    [InputTemplate(stateType = typeof(IOSGameControllerState))]
    public class IOSGameController : Gamepad
    {
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
