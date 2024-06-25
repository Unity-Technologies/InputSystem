// IMPORTANT: Auto-generated via code generator based off native headers and would sit in module since paired
//            with definitions from native code.
//
// NOTE:      Currently this has been hand-crafted for the sake of proof-of-concept purposes.

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        public static partial class GamepadUsages
        {
            public static readonly Usage LeftStick = new(436321321);
            public static readonly Usage RightStick = new(3213574);
            public static readonly Usage ButtonEast = new(64155486);
            public static readonly Usage ButtonSouth = new(2313185468);
            public static readonly Usage ButtonWest = new(12312312);
            public static readonly Usage ButtonNorth = new(2123123468);
            public static readonly Usage RumbleHaptic = new(2521315);
        }
    }

    // NOTE: Auto-generated from C struct definition. Aliased controls are basically C# counterpart of union.
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct GamepadState
    {
        // NOTE: Auto-generated from C enum with attribute type constraint since exported type depends on it.
        [Flags]
        [Serializable]
        public enum GamepadButton : uint
        {
            None = 0,                   // 0
            ButtonSouth = 1 << 0,       // 1
            ButtonWest = 1 << 1,        // 2
            ButtonEast = 1 << 2,        // 4
            ButtonNorth = 1 << 3,       // 8
            LeftPaddle = 1 << 4,        // 16
            RightPaddle = 1 << 5,       // 32
            LeftStickHat = 1 << 6,      // 64
            RightStickHat = 1 << 7,     // 128
        }
        
        [FieldOffset(0)] public GamepadButton value;    // Byte 0-3
        [FieldOffset(4)] public float leftStickX;       // Byte 4-5
        [FieldOffset(6)] public float leftStickY;       // Byte 6-7
        [FieldOffset(4)] public Vector2 leftStick;      // Byte 4-7 (Aliased)
        [FieldOffset(8)] public float rightStickX;      // Byte 8-9
        [FieldOffset(10)] public float rightStickY;     // Byte 10-11
        [FieldOffset(8)] public Vector2 rightStick;     // Byte 8-11 (Aliased)
        [FieldOffset(12)] public float leftTrigger;     // Byte 12-13
        [FieldOffset(14)] public float rightTrigger;    // Byte 14-15

        // Convenience accessors for individual buttons for this device model generated based on enum being
        // flagged for bit-flag access.
        public bool buttonSouth => 0 != (value & GamepadButton.ButtonSouth);
        public bool buttonWest => 0 != (value & GamepadButton.ButtonWest);
        public bool buttonEast => 0 != (value & GamepadButton.ButtonEast);
        public bool buttonNorth => 0 != (value & GamepadButton.ButtonNorth);
        public bool leftPaddle => 0 != (value & GamepadButton.LeftPaddle);
        public bool rightPaddle => 0 != (value & GamepadButton.RightPaddle);
        public bool leftStickHat => 0 != (value & GamepadButton.LeftStickHat);
        public bool rightStickHat => 0 != (value & GamepadButton.RightStickHat);
        
        // Convenience accessors for value type fields
    }
    
    // TODO Should we skip doing this and let Roslyn generate it for us?
    
    /// <summary>
    /// Represents the binding surface of a standard-model Gamepad.
    /// </summary>
    /// <remarks>
    /// Auto-generated from native code standard model usage definitions. This replaced current binding syntax, e.g.
    /// "Gamepad/leftStick".
    /// </remarks>
    [InputSource]
    public readonly struct Gamepad
    {
        //private Stream<GamepadState> m_Stream;
        // TODO Add API to fetch Gamepad instances via Context as well as instance specific getters for actual control representations
        
        //public readonly ref GamepadState => 
        public static readonly ObservableInput<Vector2> LeftStick = new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick");
        public static readonly ObservableInput<Vector2> RightStick = new(Usages.GamepadUsages.RightStick, "Gamepad.RightStick");
        public static readonly ObservableInput<bool> ButtonSouth = new(Usages.GamepadUsages.ButtonSouth, "Gamepad.ButtonSouth");
        public static readonly ObservableInput<bool> ButtonEast = new(Usages.GamepadUsages.ButtonEast, "Gamepad.ButtonEast");
        public static readonly ObservableInput<bool> ButtonNorth = new(Usages.GamepadUsages.ButtonNorth, "Gamepad.ButtonNorth");
        public static readonly ObservableInput<bool> ButtonWest = new(Usages.GamepadUsages.ButtonWest, "Gamepad.ButtonWest");
        
        public static OutputBindingTarget<float> RumbleHaptic = new(Usages.GamepadUsages.RumbleHaptic); // TODO Move to HapticDevice
    }
}
