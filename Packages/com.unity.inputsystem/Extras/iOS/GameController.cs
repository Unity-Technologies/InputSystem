using System.Runtime.InteropServices;
using ISX.Controls;
using ISX.LowLevel;
using ISX.Utilities;
using UnityEngine;

namespace ISX.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GameControllerState : IInputStateTypeInfo
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
        
        [InputControl(name = "buttonSouth", template = "Button", bit = (uint)Button.A)]
        [InputControl(name = "buttonWest", template = "Button", bit = (uint)Button.X)]
        [InputControl(name = "buttonNorth", template = "Button", bit = (uint)Button.Y)]
        [InputControl(name = "buttonEast", template = "Button", bit = (uint)Button.B)]
        [InputControl(name = "leftStickPress", template = "Button", bit = (uint)Button.LeftStick)]
        [InputControl(name = "rightStickPress", template = "Button", bit = (uint)Button.RightStick)]
        [InputControl(name = "leftShoulder", template = "Button", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", template = "Button", bit = (uint)Button.RightShoulder)]
        public uint buttons;
        public fixed float buttonValues[kMaxButtons];
        
        public fixed float axisValues[kMaxAxis];
        
        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputTemplate(stateType = typeof(GameControllerState))]
    public class GameController : Gamepad
    {
        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);
        }
    }
}