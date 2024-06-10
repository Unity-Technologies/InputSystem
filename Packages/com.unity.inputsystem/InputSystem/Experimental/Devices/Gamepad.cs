using System;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    // Auto-generated via code generator based off native headers.
    [Flags]
    enum GamepadButton
    {
        None = 0,
        ButtonSouth = 1,
        ButtonWest = 2,
        ButtonEast = 3,
        ButtonNorth = 4,
        LeftPaddle = 5,
        RightPaddle = 6,
        LeftStickHat = 7,
        RightStickHat = 8,
    }
    
    // Auto-generated via code generator based off native headers.
    public struct Gamepad
    {
        // TODO Add API to fetch Gamepad instances via Context as well as instance specific getters for actual control representations

        public static ObservableInput<Vector2> leftStick = new(Usages.GamepadUsages.leftStick);
        public static ObservableInput<Vector2> rightStick = new(Usages.GamepadUsages.rightStick);
        public static ObservableInput<bool> buttonSouth => new(Usages.GamepadUsages.buttonSouth);
        public static ObservableInput<bool> buttonEast = new(Usages.GamepadUsages.buttonEast);
        public static ObservableInput<bool> buttonNorth = new(Usages.GamepadUsages.buttonNorth);
        public static ObservableInput<bool> buttonWest = new(Usages.GamepadUsages.buttonWest);
        
        public static OutputBindingTarget<float> rumbleHaptic = new(Usages.GamepadUsages.rumbleHaptic); // TODO Move to HapticDevice
    }
}
