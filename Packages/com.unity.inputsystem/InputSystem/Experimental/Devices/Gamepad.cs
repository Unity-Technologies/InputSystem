using System;
using System.Runtime.InteropServices;

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
    
    // Generated from native code
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct GamepadState
    {
        [FieldOffset(0)] public uint value;
        [FieldOffset(4)] public float leftStickX;
        [FieldOffset(6)] public float leftStickY;
        [FieldOffset(4)] public Vector2 leftStick;
        [FieldOffset(8)] public float rightStickX;
        [FieldOffset(10)] public float rightStickY;
        [FieldOffset(8)] public Vector2 rightStick;
        [FieldOffset(12)] public float leftTrigger;
        [FieldOffset(12)] public float rightTrigger;

        public bool buttonSouth => 0 != (value & (int)GamepadButton.ButtonSouth);

        public override string ToString()
        {
            return $"{nameof(value)}: {value}, {nameof(leftStickX)}: {leftStickX}, {nameof(leftStickY)}: {leftStickY}, {nameof(leftStick)}: {leftStick}, {nameof(rightStickX)}: {rightStickX}, {nameof(rightStickY)}: {rightStickY}, {nameof(rightStick)}: {rightStick}, {nameof(leftTrigger)}: {leftTrigger}, {nameof(rightTrigger)}: {rightTrigger}";
        }
    }
    
    // Auto-generated via code generator based off native headers.
    public struct Gamepad
    {
        //private Stream<GamepadState> m_Stream;
        // TODO Add API to fetch Gamepad instances via Context as well as instance specific getters for actual control representations
        
        //public readonly ref GamepadState => 
        
        public static ObservableInput<Vector2> LeftStick = new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick");     // Equivalent of current "Gamepad/leftStick"
        public static ObservableInput<Vector2> RightStick = new(Usages.GamepadUsages.RightStick, "Gamepad.RightStick");   // Equivalent of current "Gamepad/rightStick"
        public static ObservableInput<bool> buttonSouth => new(Usages.GamepadUsages.ButtonSouth, "Gamepad.buttonSouth");   // Equivalent of current "Gamepad/buttonSouth"
        public static ObservableInput<bool> ButtonEast = new(Usages.GamepadUsages.ButtonEast, "Gamepad.buttonEast");      // Equivalent of current "Gamepad/buttonEast"
        public static ObservableInput<bool> ButtonNorth = new(Usages.GamepadUsages.ButtonNorth, "Gamepad.buttonNorth");    // Equivalent of current "Gamepad/buttonNorth"
        public static ObservableInput<bool> ButtonWest = new(Usages.GamepadUsages.ButtonWest, "Gamepad.buttonWest");      // Equivalent of current "Gamepad/buttonWest"
        
        public static OutputBindingTarget<float> RumbleHaptic = new(Usages.GamepadUsages.RumbleHaptic); // TODO Move to HapticDevice
    }
}
