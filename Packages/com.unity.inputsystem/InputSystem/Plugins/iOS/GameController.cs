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
        
        [InputControl(name = "dpad", template = "Dpad")]
        [InputControl(name = "dpad/up", template = "Button", bit = (uint)Button.DpadUp)]
        [InputControl(name = "dpad/right", template = "Button", bit = (uint)Button.DpadRight)]
        [InputControl(name = "dpad/down", template = "Button", bit = (uint)Button.DpadDown)]
        [InputControl(name = "dpad/left", template = "Button", bit = (uint)Button.DpadLeft)]  
        [InputControl(name = "buttonSouth", template = "Button", bit = (uint)Button.A)]
        [InputControl(name = "buttonWest", template = "Button", bit = (uint)Button.X)]
        [InputControl(name = "buttonNorth", template = "Button", bit = (uint)Button.Y)]
        [InputControl(name = "buttonEast", template = "Button", bit = (uint)Button.B)]
        [InputControl(name = "leftStickPress", template = "Button", bit = (uint)Button.LeftStick)]
        [InputControl(name = "rightStickPress", template = "Button", bit = (uint)Button.RightStick)]
        [InputControl(name = "leftShoulder", template = "Button", bit = (uint)Button.LeftShoulder)]
        [InputControl(name = "rightShoulder", template = "Button", bit = (uint)Button.RightShoulder)]
        [InputControl(name = "leftTrigger", template = "Button", bit = (uint)Button.LeftTrigger)]
        [InputControl(name = "rightTrigger", template = "Button", bit = (uint)Button.RightTrigger)]
        public uint buttons;
        public fixed float buttonValues[kMaxButtons];

        private const uint kAxisOffset = sizeof(uint) + sizeof(float) * kMaxButtons;
        [InputControl(name = "leftStick", template = "Stick", format = "VC2F")]
        [InputControl(name = "leftStick/x", format = "FLT", offset = (uint)Axis.LeftStickX * sizeof(float) + kAxisOffset)]
        [InputControl(name = "leftStick/y", format = "FLT", offset = (uint)Axis.LeftStickY * sizeof(float) + kAxisOffset)]
        [InputControl(name = "rightStick", template = "Stick", format = "VC2F")]
        [InputControl(name = "rightStick/x", format = "FLT", offset = (uint)Axis.RightStickX * sizeof(float) + kAxisOffset)]
        [InputControl(name = "rightStick/y", format = "FLT", offset = (uint)Axis.RightStickY * sizeof(float) + kAxisOffset)]
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