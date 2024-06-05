using System;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    // Auto-generated via code generator based off native headers.
    [Flags]
    enum GamepadButton
    {
        ButtonSouth = 1,
        ButtonWest = 2,
        ButtonEast = 3,
        ButtonNorth = 4,
        LeftPaddle = 5,
        RightPaddle = 6,
        LeftStickHat = 7,
        RightStickHat = 8
    }

    // Auto-generated via code generator based off native headers.
    public struct Gamepad
    {
        public static InputBindingSource<Vector2> leftStick = new(Usages.Gamepad.leftStick);
        public static InputBindingSource<Vector2> rightStick = new(Usages.Gamepad.rightStick);
        public static InputBindingSource<Button> buttonSouth => new(Usages.Gamepad.buttonSouth);
        public static InputBindingSource<bool> buttonEast = new(Usages.Gamepad.buttonEast);
        public static InputBindingSource<bool> buttonNorth = new(Usages.Gamepad.buttonNorth);
        public static OutputBindingTarget<float> rumbleHaptic = new(Usages.Gamepad.rumbleHaptic); // TODO Move to HapticDevice
    }
}
