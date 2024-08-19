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
            
            public static readonly Usage LeftTrigger = new(699321321);
            public static readonly Usage RightTrigger = new(921359974);
            
            public static readonly Usage ButtonEast = new(64155486);
            public static readonly Usage ButtonSouth = new(2313185468);
            public static readonly Usage ButtonWest = new(12312312);
            public static readonly Usage ButtonNorth = new(2123123468);
            public static readonly Usage Select = new(852255456);
            public static readonly Usage Start = new(678345453);
            public static readonly Usage Up = new(674442313);
            public static readonly Usage Down = new(91911556);
            public static readonly Usage Left = new(8217333);
            public static readonly Usage Right = new(9511593);
            public static readonly Usage LeftShoulder = new(9511593);
            public static readonly Usage RightShoulder = new(9511593);
            public static readonly Usage RumbleHaptic = new(2521315);
        }
    }

    public class ButtonAttribute : Attribute
    {
        // TODO Might need absolute/relative flags?
        // TODO Might need on/off values?

        public Type enumType { get; set; }
    }

    public class AxisAttribute : Attribute
    {
        
    }
    
    public class Axis2Attribute : Attribute
    {
        
    }

    public class NormalizedValueAttribute : Attribute
    {
        
    }
    
    // NOTE: Auto-generated from C struct definition. Aliased controls are basically C# counterpart of union.
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack=1)]
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
            LeftShoulder = 1 << 8,      // 256
            RightShoulder = 1 << 9,     // 512
            Start = 1 << 10,            // 1024
            Select = 1 << 11,           // 2048
        }
        
        [Button] [FieldOffset(0)] public GamepadButton buttons;  // Byte 0-3
        [Axis] [FieldOffset(4)] public float leftStickX;       // Byte 4-5
        [Axis] [FieldOffset(6)] public float leftStickY;       // Byte 6-7
        [Axis2] [FieldOffset(4)] public Vector2 leftStick;      // Byte 4-7 (Aliased)
        [Axis] [FieldOffset(8)] public float rightStickX;      // Byte 8-9
        [Axis] [FieldOffset(10)] public float rightStickY;     // Byte 10-11
        [Axis2] [FieldOffset(8)] public Vector2 rightStick;     // Byte 8-11 (Aliased)
        [NormalizedValue] [FieldOffset(12)] public float leftTrigger;     // Byte 12-13
        [NormalizedValue] [FieldOffset(14)] public float rightTrigger;    // Byte 14-15

        // Convenience accessors for individual buttons for this device model generated based on enum being
        // flagged for bit-flag access.
        public bool buttonSouth
        {
            get => 0 != (buttons & GamepadButton.ButtonSouth);
            set
            {
                if (value)
                    buttons |= GamepadButton.ButtonSouth;
                else
                    buttons &= ~GamepadButton.ButtonSouth;
            } 
        }

        public bool buttonWest
        {
            get => 0 != (buttons & GamepadButton.ButtonWest);
            set
            {
                if (value)
                    buttons |= GamepadButton.ButtonWest;
                else
                    buttons &= ~GamepadButton.ButtonWest;
            }
        }
        
        public bool buttonEast 
        {
            get => 0 != (buttons & GamepadButton.ButtonEast);
            set
            {
                if (value)
                    buttons |= GamepadButton.ButtonEast;
                else
                    buttons &= ~GamepadButton.ButtonEast;
            }
        }
        
        public bool buttonNorth 
        {
            get => 0 != (buttons & GamepadButton.ButtonNorth);
            set
            {
                if (value)
                    buttons |= GamepadButton.ButtonNorth;
                else
                    buttons &= ~GamepadButton.ButtonNorth;
            }
        }
        
        public bool leftPaddle 
        {
            get => 0 != (buttons & GamepadButton.ButtonNorth);
            set
            {
                if (value)
                    buttons |= GamepadButton.ButtonNorth;
                else
                    buttons &= ~GamepadButton.ButtonNorth;
            }
        }
        
        public bool rightPaddle 
        {
            get => 0 != (buttons & GamepadButton.RightPaddle);
            set
            {
                if (value)
                    buttons |= GamepadButton.RightPaddle;
                else
                    buttons &= ~GamepadButton.RightPaddle;
            }
        }
        
        public bool leftStickHat 
        {
            get => 0 != (buttons & GamepadButton.LeftStickHat);
            set
            {
                if (value)
                    buttons |= GamepadButton.LeftStickHat;
                else
                    buttons &= ~GamepadButton.LeftStickHat;
            }
        }
        
        public bool rightStickHat 
        {
            get => 0 != (buttons & GamepadButton.RightStickHat);
            set
            {
                if (value)
                    buttons |= GamepadButton.RightStickHat;
                else
                    buttons &= ~GamepadButton.RightStickHat;
            }
        }
        
        // Convenience accessors for value type fields
    }
    
    // TODO Should we skip doing this and let Roslyn generate it for us?

    /*struct IndexedDevice<T>
    {
        private readonly ushort m_DeviceID;
        
        public T this[int instance]
        {
            get
            {
                
            }
        }
    }*/
    
    /// <summary>
    /// Represents the binding surface of a standard-model Gamepad.
    /// </summary>
    /// <remarks>
    /// Auto-generated from native code standard model usage definitions. This replaced current binding syntax, e.g.
    /// "Gamepad/leftStick".
    /// </remarks>
    [InputSource]
    public readonly struct Gamepad // TODO A gamepad instance itself should also be an observable input node
    {
        // Gamepad.LeftStick.Subscribe(...)
        
        public static Gamepad any => new Gamepad(); // TODO Should this be an observable current gamepad device?
        
        //private Stream<GamepadState> m_Stream;
        // TODO Add API to fetch Gamepad instances via Context as well as instance specific getters for actual control representations
        
        // TODO Consider getting rid of displayName
        
        //public readonly ObservableInput<Vector2> LeftStick = new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick");

        
        //public readonly ref GamepadState => 
        public static ObservableInputNode<Vector2> LeftStick => new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick");
        public static readonly ObservableInputNode<Vector2> RightStick = new(Usages.GamepadUsages.RightStick, "Gamepad.RightStick");
        
        public static readonly ObservableInputNode<float> LeftTrigger = new(Usages.GamepadUsages.LeftTrigger, "Gamepad.LeftTrigger");
        public static readonly ObservableInputNode<float> RightTrigger = new(Usages.GamepadUsages.RightTrigger, "Gamepad.RightTrigger");
        
        public static readonly ObservableInputNode<bool> ButtonSouth = new(Usages.GamepadUsages.ButtonSouth, "Gamepad.ButtonSouth");
        public static readonly ObservableInputNode<bool> ButtonEast = new(Usages.GamepadUsages.ButtonEast, "Gamepad.ButtonEast");
        public static readonly ObservableInputNode<bool> ButtonNorth = new(Usages.GamepadUsages.ButtonNorth, "Gamepad.ButtonNorth");
        public static readonly ObservableInputNode<bool> ButtonWest = new(Usages.GamepadUsages.ButtonWest, "Gamepad.ButtonWest");
        public static readonly ObservableInputNode<bool> LeftShoulder = new(Usages.GamepadUsages.LeftShoulder, "Gamepad.LeftShoulder");
        public static readonly ObservableInputNode<bool> RightShoulder = new(Usages.GamepadUsages.RightShoulder, "Gamepad.RightShoulder");
        public static readonly ObservableInputNode<bool> Select = new(Usages.GamepadUsages.Select, "Gamepad.Select");
        public static readonly ObservableInputNode<bool> Start = new(Usages.GamepadUsages.Start, "Gamepad.Start");
        public static readonly ObservableInputNode<bool> Up = new(Usages.GamepadUsages.Up, "Gamepad.Up");
        public static readonly ObservableInputNode<bool> Left = new(Usages.GamepadUsages.Left, "Gamepad.Left");
        public static readonly ObservableInputNode<bool> Right = new(Usages.GamepadUsages.Right, "Gamepad.Right");
        public static readonly ObservableInputNode<bool> Down = new(Usages.GamepadUsages.Down, "Gamepad.Down");
        
        public static OutputBindingTarget<float> RumbleHaptic = new(Usages.GamepadUsages.RumbleHaptic); // TODO Move to HapticDevice
    }
}
