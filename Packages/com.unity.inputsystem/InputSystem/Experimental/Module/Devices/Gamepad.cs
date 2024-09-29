// IMPORTANT: Auto-generated via code generator based off native headers and would sit in module since paired
//            with definitions from native code.
//
// NOTE:      Currently this has been hand-crafted for the sake of proof-of-concept purposes.

using System;
using System.Collections;
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
            public static readonly Usage LeftStickHat = new(123897136);
            public static readonly Usage RightStickHat = new(923897136);
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
        public enum GamepadButtonBitShift : int
        {
            South = 0,
            West = 1,
            East = 2,
            North = 3,
            LeftPaddle = 4,
            RightPaddle = 5,
            LeftStickHat = 6,
            RightStickHat = 7,
            LeftShoulder = 8,
            RightShoulder = 9,
            Start = 10,
            Select = 11
        }
        
        // NOTE: Auto-generated from C enum with attribute type constraint since exported type depends on it.
        [Flags]
        [Serializable]
        public enum GamepadButton : uint
        {
            None = 0,                                                  // 0
            South = 1 << GamepadButtonBitShift.South,                  // 1
            West = 1 << GamepadButtonBitShift.West,                    // 2
            East = 1 << GamepadButtonBitShift.East,                    // 4
            North = 1 << GamepadButtonBitShift.North,                  // 8
            LeftPaddle = 1 << GamepadButtonBitShift.LeftPaddle,        // 16
            RightPaddle = 1 << GamepadButtonBitShift.RightPaddle,      // 32
            LeftStickHat = 1 << GamepadButtonBitShift.LeftStickHat,    // 64
            RightStickHat = 1 << GamepadButtonBitShift.RightStickHat,  // 128
            LeftShoulder = 1 << GamepadButtonBitShift.LeftShoulder,    // 256
            RightShoulder = 1 << GamepadButtonBitShift.RightShoulder,  // 512
            Start = 1 << GamepadButtonBitShift.Start,                  // 1024
            Select = 1 << GamepadButtonBitShift.Select,                // 2048
        }
        
        [Button] [FieldOffset(0)] public GamepadButton buttons;        // Byte 0-3
        [Axis] [FieldOffset(4)] public float leftStickX;               // Byte 4-5
        [Axis] [FieldOffset(6)] public float leftStickY;               // Byte 6-7
        [Axis2] [FieldOffset(4)] public Vector2 leftStick;             // Byte 4-7 (Aliased)
        [Axis] [FieldOffset(8)] public float rightStickX;              // Byte 8-9
        [Axis] [FieldOffset(10)] public float rightStickY;             // Byte 10-11
        [Axis2] [FieldOffset(8)] public Vector2 rightStick;            // Byte 8-11 (Aliased)
        [NormalizedValue] [FieldOffset(12)] public float leftTrigger;  // Byte 12-13
        [NormalizedValue] [FieldOffset(14)] public float rightTrigger; // Byte 14-15

        // Convenience accessors for individual buttons for this device model generated based on enum being
        // flagged for bit-flag access.
        public bool buttonSouth
        {
            get => 0 != (buttons & GamepadButton.South);
            set
            {
                if (value)
                    buttons |= GamepadButton.South;
                else
                    buttons &= ~GamepadButton.South;
            } 
        }

        public bool buttonWest
        {
            get => 0 != (buttons & GamepadButton.West);
            set
            {
                if (value)
                    buttons |= GamepadButton.West;
                else
                    buttons &= ~GamepadButton.West;
            }
        }
        
        public bool buttonEast 
        {
            get => 0 != (buttons & GamepadButton.East);
            set
            {
                if (value)
                    buttons |= GamepadButton.East;
                else
                    buttons &= ~GamepadButton.East;
            }
        }

        public bool buttonNorth 
        {
            get => 0 != (buttons & GamepadButton.North);
            set
            {
                if (value)
                    buttons |= GamepadButton.North;
                else
                    buttons &= ~GamepadButton.North;
            }
        }
        
        public bool leftPaddle 
        {
            get => 0 != (buttons & GamepadButton.North);
            set
            {
                if (value)
                    buttons |= GamepadButton.North;
                else
                    buttons &= ~GamepadButton.North;
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

    // TODO We should generate this from the state definition
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

        // TODO Need to make a decision
        // Gamepad.any.leftStick;
        // Gamepad.gamepads[0].leftStick;
        
        /// <summary>
        /// Returns all currently connected <c>Gamepad</c> devices on the system (if any).
        /// </summary>
        /// <remarks>
        /// The return value is never null.
        /// </remarks>
        public static ReadOnlySpan<Gamepad> devices => GetDevices(Context.instance);
        
        /// <summary>
        /// Returns all currently connected <c>Gamepad</c> devices on the system from the perspective of the
        /// given context.
        /// </summary>
        /// <param name="context">The context for which to retrieve devices.</param>
        /// <returns>ReadOnlySpan&lt;Gamepad&gt; containing all available devices.</returns>
        public static ReadOnlySpan<Gamepad> GetDevices(Context context) => context.GetDevices<Gamepad>(); 
        
        //private Stream<GamepadState> m_Stream;
        // TODO Add API to fetch Gamepad instances via Context as well as instance specific getters for actual control representations
        
        // TODO Consider getting rid of displayName
        
        //public readonly ObservableInput<Vector2> LeftStick = new(Usages.GamepadUsages.LeftStick, "Gamepad.LeftStick");
        
        private static readonly ObservableInput<bool>[] Buttons = 
        {
            new (Endpoint.FromUsage(Usages.GamepadUsages.ButtonSouth), "Gamepad.ButtonSouth"), 
            new(Endpoint.FromUsage(Usages.GamepadUsages.ButtonEast), "Gamepad.ButtonEast"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.ButtonWest), "Gamepad.ButtonWest"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.ButtonNorth), "Gamepad.ButtonNorth"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.LeftShoulder), "Gamepad.LeftShoulder"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.RightShoulder), "Gamepad.RightShoulder"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.Select), "Gamepad.Select"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.Start), "Gamepad.Start"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.Up), "Gamepad.Up"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.Left), "Gamepad.Left"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.Right), "Gamepad.Right"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.Down), "Gamepad.Down"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.LeftStickHat), "Gamepad.LeftStickHat"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.RightStickHat), "Gamepad.RightStickHat")
        };
        private static readonly ObservableInput<Vector2>[] Sticks = 
        {
            new(Endpoint.FromUsage(Usages.GamepadUsages.LeftStick), "Gamepad.LeftStick"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.RightStick), "Gamepad.RightStick")
        };

        private static readonly ObservableInput<float>[] Values =
        {
            new(Endpoint.FromUsage(Usages.GamepadUsages.LeftTrigger), "Gamepad.LeftTrigger"),
            new(Endpoint.FromUsage(Usages.GamepadUsages.RightTrigger), "Gamepad.RightTrigger")
        };

        #region Control type accessors
        
        public static ReadOnlySpan<ObservableInput<bool>> buttons => new (Buttons); // TODO Return type would actually be a custom collection type
        public static ReadOnlySpan<ObservableInput<Vector2>> sticks => new (Sticks);
        //public static ReadOnlySpan<ObservableInputNode<float>> values = new (Values); // TODO What is the issue?
        
        #endregion
        
        public static ObservableInput<Vector2> leftStick => Sticks[0]; // TODO This should be a specific ObservableInputNodeType allowing access to underlying
        public static readonly ObservableInput<Vector2> RightStick = Sticks[1];
        
        public static readonly ObservableInput<float> LeftTrigger = new(Endpoint.FromUsage(Usages.GamepadUsages.LeftTrigger), "Gamepad.LeftTrigger");
        public static readonly ObservableInput<float> RightTrigger = new(Endpoint.FromUsage(Usages.GamepadUsages.RightTrigger), "Gamepad.RightTrigger");
        
        public static readonly ObservableInput<bool> ButtonSouth = Buttons[0];
        public static readonly ObservableInput<bool> ButtonEast = Buttons[1];
        public static readonly ObservableInput<bool> ButtonNorth = Buttons[2];
        public static readonly ObservableInput<bool> ButtonWest = Buttons[3];
        public static readonly ObservableInput<bool> LeftShoulder = Buttons[4];
        public static readonly ObservableInput<bool> RightShoulder = Buttons[5];
        public static readonly ObservableInput<bool> Select = Buttons[6];
        public static readonly ObservableInput<bool> Start = Buttons[7];
        public static readonly ObservableInput<bool> Up = Buttons[8];
        public static readonly ObservableInput<bool> Left = Buttons[9];
        public static readonly ObservableInput<bool> Right = Buttons[10];
        public static readonly ObservableInput<bool> Down = Buttons[11];
        public static readonly ObservableInput<bool> LeftStickHat = Buttons[12];
        public static readonly ObservableInput<bool> RightStickHat = Buttons[13];
    }

    public class GamepadSettings
    {
        // TODO Should we 
    }
}
